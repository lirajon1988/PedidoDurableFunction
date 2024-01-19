using System;
using System.Collections.Generic;
using System.Net.Http;
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
        public static async Task<string> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
           try
            {
                var pedido = context.GetInput<Pedido>();
                var outputs = new List<string>();
                
                // Replace "hello" with the name of your Durable Activity Function.
                outputs.Add(await context.CallActivityAsync<string>(nameof(Receber), pedido));
                outputs.Add(await context.CallActivityAsync<string>(nameof(Baixar), pedido));
                outputs.Add(await context.CallActivityAsync<string>(nameof(Entregar), pedido));

                return await context.CallActivityAsync<string>(nameof(Visualizar), outputs);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

         [FunctionName(nameof(Receber))]
        public static string Receber([ActivityTrigger] Pedido pedido, ILogger log)
        {
            log.LogInformation($"Nome: {pedido.NomeCliente}.");
            if (pedido.Cpf.Length != 11)
                return $"Cpf do cliente {pedido.NomeCliente} inválido";
                
            return $"Pedido {pedido.NomeCliente} Recebido!";
        }

        [FunctionName(nameof(Baixar))]
        public static string Baixar([ActivityTrigger] Pedido pedido, ILogger log)
        {
            log.LogInformation($"Item: {pedido.NumeroPedido}");
            if (pedido.Itens.Count == 0)
                return $"Pedido {pedido.NumeroPedido} Não possui itens cadastrados!";

            if (pedido.Itens.Exists(x => x.Quantidade == 0))
                return $"Pedido {pedido.NumeroPedido} tem itens com a quantidade inválida!";

            return $"Pedido {pedido.NumeroPedido} baixado!";
        }

        [FunctionName(nameof(Entregar))]
        public static string Entregar([ActivityTrigger] Pedido pedido, ILogger log)
        {
            log.LogInformation($"Pedido {pedido.Cpf}");
            return $"Pedido {pedido.NumeroPedido} Entregue!";
        }

        [FunctionName(nameof(Visualizar))]
        public static string Visualizar([ActivityTrigger] List<string> retornos, ILogger log)
        {
            var resposta = JsonConvert.SerializeObject(retornos);
            log.LogInformation($"Respostas do processamento {resposta}");
            return resposta;
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
            //log.LogInformation($"Request enviado: {payload}", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId, true);
        }

        public class Pedido
        {
            public int NumeroPedido { get; set; }
            public string NomeCliente { get; set; }
            public string Cpf { get; set; }
            public List<ItemPedido> Itens {get;set;}
            
        }

        public class ItemPedido
        {
            public int IdItem {get;set;}
            public string Descricao { get; set; }
            public int Quantidade { get; set; }
        }
    }
}