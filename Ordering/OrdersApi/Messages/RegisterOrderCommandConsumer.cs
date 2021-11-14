using MassTransit.Riders;
using MassTransit;
using Messaging.InterfacesConstants.Commands;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Net.Http.Headers;
using OrdersApi.Models;
using OrdersApi.Persistence;

namespace OrdersApi.Messages
{
    public class RegisterOrderCommandConsumer : IConsumer<IRegisterOrderCommand>
    {

        private readonly IOrderRepository _orderRepo;
        private readonly IHttpClientFactory _clientFactory;

        public RegisterOrderCommandConsumer(IOrderRepository orderRepo, IHttpClientFactory clientFactory)
        {
            _orderRepo = orderRepo;
            _clientFactory = clientFactory;

        }

        public async Task Consume(ConsumeContext<IRegisterOrderCommand> context)
        {
            var result = context.Message;

            if (result.PictureUrl != null && result.UserEmail != null && result.ImageData != null)
            {
                SaveOrder(result);

                var client = _clientFactory.CreateClient();
                Tuple<List<byte[]>, Guid> orderDetailData = await GetFacesFromFaceApiAsync(client, result.ImageData, result.OrderId);
                List<byte[]> faces = orderDetailData.Item1;
                Guid orderId = orderDetailData.Item2;
                SaveOrderDetails(orderId, faces);
            }

        }

        private async Task<Tuple<List<byte[]>, Guid>> GetFacesFromFaceApiAsync(HttpClient client, byte[] imageData, Guid orderId)
        {
            var byteContent = new ByteArrayContent(imageData);
            Tuple<List<byte[]>, Guid> orderDetailData = null;
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            using (var response = await client.PostAsync("http://localhost:6001/api/faces?orderId=" + orderId, byteContent))
            {
                string apiResponse = await response.Content.ReadAsStringAsync();
                orderDetailData = JsonConvert.DeserializeObject<Tuple<List<byte[]>, Guid>>(apiResponse);
            }

            return orderDetailData;
        }

        private void SaveOrderDetails(Guid orderId, List<byte[]> faces)
        {
            var order = _orderRepo.GetOrderAsync(orderId).Result;

            if (order != null)
            {
                order.Status = Status.Processed;
                foreach (var face in faces)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = orderId,
                        FaceData = face
                    };
                    order.OrderDetails.Add(orderDetail);
                }
                _orderRepo.UpdateOrder(order);
            }
        }

        private void SaveOrder(IRegisterOrderCommand result)
        {
            Order order = new Order
            {
                OrderId = result.OrderId,
                UserEmail = result.UserEmail,
                Status = Status.Registered,
                PictureUrl = result.PictureUrl,
                ImageData = result.ImageData
            };
            _orderRepo.RegisterOrder(order);

        }
    }
}
