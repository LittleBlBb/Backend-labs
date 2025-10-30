﻿using System.Text;
using Common;
using Models.Dto.V1.Requests;
using Models.Dto.V1.Responses;

namespace Oms.Consumer.Clients;

public class OmsClient(HttpClient client)
{
    public async Task<V1AuditLogOrderResponse> LogOrder(V1AuditLogOrderRequest request, CancellationToken token)
    {
        var msg = await client.PostAsync("api/v1/audit/log-order", new StringContent(request.ToJson(), Encoding.UTF8, "application/json"), token);
        if (msg.IsSuccessStatusCode)
        {
            var content = await msg.Content.ReadAsStringAsync(cancellationToken: token);
            return content.FromJson<V1AuditLogOrderResponse>();
        }

        throw new HttpRequestException();
    }
}