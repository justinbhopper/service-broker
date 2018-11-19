using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace ServiceBroker
{
    public class RabbitMqTest : IDisposable
    {
        private const string _queueName = "test";

        private readonly TableChangeSerializer _serializer = new TableChangeSerializer();
        private readonly string _host;
        private readonly string _username;
        private readonly string _password;

        public RabbitMqTest(string host, string username, string password)
        {
            _host = host;
            _username = username;
            _password = password;
        }
        
        public TimeSpan Execute(int numberOfMessages)
        {
            var factory = new ConnectionFactory() { HostName = _host, UserName = _username, Password = _password };
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: _queueName,
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                channel.QueuePurge(_queueName);

                channel.ConfirmSelect();

                string message = JsonConvert.SerializeObject(
                    new List<TableChangeMessage>
                    {
                        new TableChangeMessage
                        {
                            TableName = "Test",
                            Inserted = new List<TableChangeMessageItem>
                            {
                                new TableChangeMessageItem
                                {
                                    Id = 1
                                }
                            }
                        }
                    }
                );

                byte[] body = Encoding.UTF8.GetBytes(message);
                
                for (int i = 0; i < numberOfMessages; i++)
                {
                    channel.BasicPublish(exchange: "",
                        routingKey: _queueName,
                        basicProperties: null,
                        body: body);
                }

                channel.WaitForConfirms();
                
                var startTime = DateTime.Now;

                for (int i = 0; i < numberOfMessages; i++)
                {
                    var getResult = channel.BasicGet(_queueName, true);
                    
                    var request = _serializer.Deserialize(getResult.Body, Encoding.UTF8);

                    if (request.First().Inserted.First() != 1)
                        throw new Exception("Failed to read message from rabbitmq");
                }

                return DateTime.Now - startTime;
            }
        }

        public void Dispose()
        {
            // Do nothing
        }
    }
}
