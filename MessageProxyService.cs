using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleProxyBot
{
    public class MessageProxyService
    {
        private readonly ConcurrentDictionary<string, ConversationReference> _references = new ConcurrentDictionary<string, ConversationReference>();
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly string _appId;

        public MessageProxyService(IBotFrameworkHttpAdapter adapter, IConfiguration configuration)
        {
            _adapter = adapter;
            _appId = configuration["MicrosoftAppId"];
        }

        public async Task ProxyToSharedConversation(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            bool send = true;
            var reference = turnContext.Activity.GetConversationReference();
            if (!_references.ContainsKey(reference.Conversation.Id))
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("You have been added to the conversation."));
                send = false;
            }
            
            _references.AddOrUpdate(reference.Conversation.Id, reference, (key, newValue) => reference);

            if (send)
            {
                foreach (var refKeyVal in _references)
                {
                    if (refKeyVal.Key != reference.Conversation.Id)
                    {
                        var continueReference = refKeyVal.Value;
                        await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, continueReference, async (context, cancelToken) => 
                        {
                            await context.SendActivityAsync(turnContext.Activity.Text);
                        }, cancellationToken);
                    }
                }
            }
        }
    }
}
