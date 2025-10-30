using WebApplication1.DAL.Models;

namespace WebApplication1.DAL.Interfaces;
public interface IOrderRepository
{
    Task<V1OrderDal[]> BulkInsert(V1OrderDal[] model, CancellationToken token);

    Task<V1OrderDal[]> Query(QueryOrdersDalModel model, CancellationToken token);
}