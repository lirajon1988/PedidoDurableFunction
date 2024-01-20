using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Company.Function
{
    public static class PedidoDurableFunctionsOrchestration
    {
        [FunctionName("PedidoDurableFunctionsOrchestration")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {

            var pedido = context.GetInput<Pedido>();
            var outputs = new List<string>();
            try
            {
                outputs.Add(await context.CallActivityAsync<string>(nameof(Receber), pedido));
                outputs.Add(await context.CallActivityAsync<string>(nameof(Baixar), pedido));
                outputs.Add(await context.CallActivityAsync<string>(nameof(Entregar), pedido));

                //return await context.CallActivityAsync<List<string>>(nameof(Visualizar), outputs);

            }
            catch (Exception ex)
            {
                outputs.Add(ex.Message);
            }

            return outputs;
        }

        [FunctionName(nameof(Receber))]
        public static string Receber([ActivityTrigger] Pedido pedido, ILogger log)
        {

            log.LogInformation($"Nome: {pedido.NomeCliente}.");
            if (pedido.Cpf.Length != 11)
                return $"Cpf do cliente {pedido.NomeCliente} inválido";

            return $"Método Receber: Pedido {pedido.NomeCliente} Recebido!";
        }

        [FunctionName(nameof(Baixar))]
        public static string Baixar([ActivityTrigger] Pedido pedido, ILogger log)
        {
            log.LogInformation($"Item: {pedido.NumeroPedido}");
            if (pedido.Itens.Count == 0)
                return $"Método Baixar: Pedido {pedido.NumeroPedido} Não possui itens cadastrados!";

            if (pedido.Itens.Exists(x => x.Quantidade == 0))
                return $"Método Baixar: Pedido {pedido.NumeroPedido} tem itens com a quantidade inválida!";

            return $"Método Baixar: Pedido {pedido.NumeroPedido} baixado!";
        }

        [FunctionName(nameof(Entregar))]
        public static string Entregar([ActivityTrigger] Pedido pedido, ILogger log)
        {
            log.LogInformation($"Pedido {pedido.Cpf}");
            return $"Método Entregar: Pedido {pedido.NumeroPedido} Entregue!";
        }

        [FunctionName("PedidoDurableFunctionsOrchestration_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            var payload = req.Content.ReadAsStringAsync().Result.ToString();
            string instanceId = await starter.StartNewAsync("PedidoDurableFunctionsOrchestration", JsonConvert.DeserializeObject<Pedido>(payload));

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId, true);
        }

        public class Pedido
        {
            public int NumeroPedido { get; set; }
            public string NomeCliente { get; set; }
            public string Cpf { get; set; }
            public List<ItemPedido> Itens { get; set; }

        }

        public class ItemPedido
        {
            public int IdItem { get; set; }
            public string Descricao { get; set; }
            public int Quantidade { get; set; }
        }
    }
}