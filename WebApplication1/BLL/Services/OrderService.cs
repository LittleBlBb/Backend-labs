using Messages;
using Microsoft.Extensions.Options;
using Oms.Config;
using Oms.Services;
using WebApplication.BLL.Models;
using WebApplication1.DAL;
using WebApplication1.DAL.Interfaces;
using WebApplication1.DAL.Models;
using OrderItemUnit = Models.Dto.Common.OrderItemUnit;
using OrderUnit = Models.Dto.Common.OrderUnit;

namespace WebApplication1.BLL.Services;


public class OrderService(
    UnitOfWork unitOfWork,
    IOrderRepository orderRepository,
    IOrderItemRepository orderItemRepository,
    RabbitMqService rabbitMqService,
    IOptions<RabbitMqSettings> settings)
{
    public async Task<OrderUnit[]> BatchInsert(OrderUnit[] orderUnits, CancellationToken token)
{
    var now = DateTimeOffset.UtcNow;
    await using var transaction = await unitOfWork.BeginTransactionAsync(token);

    if (orderUnits == null || orderUnits.Length == 0)
        return Array.Empty<OrderUnit>();

    try
    {
        var orderDals = orderUnits.Select(order => new V1OrderDal
        {
            CustomerId = order.CustomerId,
            DeliveryAddress = order.DeliveryAddress,
            TotalPriceCents = order.TotalPriceCents,
            TotalPriceCurrency = order.TotalPriceCurrency,
            CreatedAt = now,
            UpdatedAt = now
        }).ToArray();

        var insertedOrders = await orderRepository.BulkInsert(orderDals, token);
        var resultOrders = new List<OrderUnit>();

        for (int i = 0; i < insertedOrders.Length; i++)
        {
            var orderUnit = orderUnits[i];
            var insertedOrder = insertedOrders[i];
            var orderId = insertedOrder.Id;

            var orderItemDals = orderUnit.OrderItems?.Select(item => new V1OrderItemDal
            {
                OrderId = orderId,
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                ProductTitle = item.ProductTitle,
                ProductUrl = item.ProductUrl,
                PriceCents = item.PriceCents,
                PriceCurrency = item.PriceCurrency,
                CreatedAt = now,
                UpdatedAt = now
            }).ToArray() ?? Array.Empty<V1OrderItemDal>();

            var insertedOrderItems = await orderItemRepository.BulkInsert(orderItemDals, token);

            resultOrders.Add(new OrderUnit
            {
                Id = insertedOrder.Id,
                CustomerId = insertedOrder.CustomerId,
                DeliveryAddress = insertedOrder.DeliveryAddress,
                TotalPriceCents = insertedOrder.TotalPriceCents,
                TotalPriceCurrency = insertedOrder.TotalPriceCurrency,
                CreatedAt = now,
                UpdatedAt = now,
                OrderItems = insertedOrderItems.Select(item => new OrderItemUnit
                {
                    Id = item.Id,
                    OrderId = item.OrderId,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    ProductTitle = item.ProductTitle,
                    ProductUrl = item.ProductUrl,
                    PriceCents = item.PriceCents,
                    PriceCurrency = item.PriceCurrency,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt
                }).ToArray()
            });
        }

        // --- Коммитим изменения в БД ---
        await transaction.CommitAsync(token);

        // --- После успешного коммита публикуем в RabbitMQ ---
        try
        {
            var messages = resultOrders.Select(order => new OrderCreatedMessage
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                DeliveryAddress = order.DeliveryAddress,
                TotalPriceCents = order.TotalPriceCents,
                TotalPriceCurrency = order.TotalPriceCurrency,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt,
                OrderItems = order.OrderItems.Select(oi => new OrderItemUnit
                {
                    Id = oi.Id,
                    OrderId = oi.OrderId,
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity,
                    ProductTitle = oi.ProductTitle,
                    ProductUrl = oi.ProductUrl,
                    PriceCents = oi.PriceCents,
                    PriceCurrency = oi.PriceCurrency,
                    CreatedAt = oi.CreatedAt,
                    UpdatedAt = oi.UpdatedAt
                }).ToArray()
            }).ToArray();

            await rabbitMqService.Publish(messages, settings.Value.OrderCreatedQueue, token);
            Console.WriteLine($"[RabbitMQ] Published {messages.Length} order(s) to queue '{settings.Value.OrderCreatedQueue}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RabbitMQ] Failed to publish message: {ex.Message}");
            // Не трогаем транзакцию — БД уже зафиксирована.
        }

        return resultOrders.ToArray();
    }
    catch (Exception)
    {
        // Rollback только если транзакция ещё не завершена
        try
        {
            await transaction.RollbackAsync(token);
        }
        catch (InvalidOperationException)
        {
            // Игнорируем "Transaction completed" — это нормально
        }

        throw;
    }
}


    /// <summary>
    /// Метод получения заказов
    /// </summary>
    public async Task<OrderUnit[]> GetOrders(QueryOrderItemsModel model, CancellationToken token)
    {
        var orders = await orderRepository.Query(new QueryOrdersDalModel
        {
            Ids = model.Ids,
            CustomerIds = model.CustomerIds,
            Limit = model.PageSize,
            Offset = (model.Page - 1) * model.PageSize
        }, token);

        if (orders.Length is 0)
        {
            return [];
        }
        
        ILookup<long, V1OrderItemDal> orderItemLookup = null;
        if (model.IncludeOrderItems)
        {
            var orderItems = await orderItemRepository.Query(new QueryOrderItemsDalModel
            {
                OrderIds = orders.Select(x => x.Id).ToArray(),
            }, token);

            orderItemLookup = orderItems.ToLookup(x => x.OrderId);
        }

        return Map(orders, orderItemLookup);
    }
    
    private OrderUnit[] Map(V1OrderDal[] orders, ILookup<long, V1OrderItemDal> orderItemLookup = null)
    {
        return orders.Select(x => new OrderUnit
        {
            Id = x.Id,
            CustomerId = x.CustomerId,
            DeliveryAddress = x.DeliveryAddress,
            TotalPriceCents = x.TotalPriceCents,
            TotalPriceCurrency = x.TotalPriceCurrency,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            OrderItems = orderItemLookup?[x.Id].Select(o => new OrderItemUnit
            {
                Id = o.Id,
                OrderId = o.OrderId,
                ProductId = o.ProductId,
                Quantity = o.Quantity,
                ProductTitle = o.ProductTitle,
                ProductUrl = o.ProductUrl,
                PriceCents = o.PriceCents,
                PriceCurrency = o.PriceCurrency,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            }).ToArray() ?? []
        }).ToArray();
    }
}