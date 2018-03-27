using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;
using Serilog;

namespace ChannelAdvisor_Api.Helpers
{
    public class ChannelAdvisorLogger : IEndpointBehavior, IClientMessageInspector
    {
        private readonly ILogger logger;

        public ChannelAdvisorLogger(ILogger logger)
        {
            this.logger = logger;
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {

        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            //create buffered copy of message 
            MessageBuffer buffer = reply.CreateBufferedCopy(Int32.MaxValue);
            // pass back the original message
            reply = buffer.CreateMessage();

            //create a second copy of message
            Message msg = buffer.CreateMessage();
            StringBuilder builder = new StringBuilder();

            using (var writer = XmlWriter.Create(builder))
            {
                msg.WriteBody(writer);
            }

            string strMessage = builder.ToString();

            try
            {
                string fileName = string.Format("ChannelAdvisor_{0}.xml", DateTime.Now.ToFileTime());

                using (var streamWriter = new StreamWriter(fileName, true))
                {
                    streamWriter.WriteLine(strMessage);
                }

                this.logger.Information("Received file {0}", fileName);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Exception at saving ChannelAdvisor orders file");
            }
        }


        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(this);
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {

        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            return null;
        }

        public void Validate(ServiceEndpoint endpoint)
        {

        }
    }
}
