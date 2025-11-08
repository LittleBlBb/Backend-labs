using Models.Dto.V1.Requests;
using Models.Dto.V1.Responses;
using WebApplication1.BLL.Models;
using WebApplication1.Validators;
using OrderItemUnit = Models.Dto.Common.OrderItemUnit;
using OrderUnit = Models.Dto.Common.OrderUnit;

namespace WebApplication1.Controllers.V1;
using BLL.Services;
using Microsoft.AspNetCore.Mvc;
[Route("api/v1/order")]
public class OrderController: ControllerBase
{
    private readonly ValidatorFactory _validatorFactory;
    private readonly OrderService _orderService;

    public OrderController(ValidatorFactory validatorFactory, OrderService orderService)
    {
        _validatorFactory = validatorFactory;
        _orderService = orderService;
    }

    [HttpPost("batch-create")]
    public async Task<ActionResult<V1CreateOrderResponse>> V1BatchCreate([FromBody] V1CreateOrderRequest request, CancellationToken token)
    {
        var validationResult = await _validatorFactory.GetValidator<V1CreateOrderRequest>().ValidateAsync(request, token);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }
        
        var res = await _orderService.BatchInsert(request.Orders.Select(x => new OrderUnit
        {
            CustomerId = x.CustomerId,
            DeliveryAddress = x.DeliveryAddress,
            TotalPriceCents = x.TotalPriceCents,
            TotalPriceCurrency = x.TotalPriceCurrency,
            OrderItems = x.OrderItems.Select(p => new OrderItemUnit
            {
                ProductId = p.ProductId,
                Quantity = p.Quantity,
                ProductTitle = p.ProductTitle,
                ProductUrl = p.ProductUrl,
                PriceCents = p.PriceCents,
                PriceCurrency = p.PriceCurrency,
            }).ToArray()
        }).ToArray(), token);


        return Ok(new V1CreateOrderResponse
        {
            Orders = Map(res)
        });
    }

    [HttpPost("query")]
    public async Task<ActionResult<V1QueryOrdersResponse>> V1QueryOrders([FromBody] V1QueryOrdersRequest request, CancellationToken token)
    {
        var validationResult = await _validatorFactory.GetValidator<V1QueryOrdersRequest>().ValidateAsync(request, token);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.ToDictionary());
        }
        
        var res = await _orderService.GetOrders(new QueryOrderItemsModel
        {
            Ids = request.Ids,
            CustomerIds = request.CustomerIds,
            Page = request.Page ?? 0,
            PageSize = request.PageSize ?? 0,
            IncludeOrderItems = request.IncludeOrderItems
        }, token);
        
        return Ok(new V1QueryOrdersResponse
        {
            Orders = Map(res)
        });
    }
    private Models.Dto.Common.OrderUnit[] Map(OrderUnit[] orders)
    {
        return orders.Select(x => new Models.Dto.Common.OrderUnit
        {
            Id = x.Id,
            CustomerId = x.CustomerId,
            DeliveryAddress = x.DeliveryAddress,
            TotalPriceCents = x.TotalPriceCents,
            TotalPriceCurrency = x.TotalPriceCurrency,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            OrderItems = x.OrderItems.Select(p => new Models.Dto.Common.OrderItemUnit
            {
                Id = p.Id,
                OrderId = p.OrderId,
                ProductId = p.ProductId,
                Quantity = p.Quantity,
                ProductTitle = p.ProductTitle,
                ProductUrl = p.ProductUrl,
                PriceCents = p.PriceCents,
                PriceCurrency = p.PriceCurrency,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToArray()
        }).ToArray();
    }
}