﻿# SimpleProxyBot

Built from Bot Framework v4 echo bot sample.

It demonstrates how to add users from any channel to a dictionary, then forward messages from every user to all users.

## Prerequisites

- [.NET Core SDK](https://dotnet.microsoft.com/download) version 3.1

  ```bash
  # determine dotnet version
  dotnet --version
  ```


```cs
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
```