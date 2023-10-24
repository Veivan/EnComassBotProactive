// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.BotBuilderSamples.Models;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotBuilderSamples.Controllers
{
    [Route("api/notify")]
    [ApiController]
    public class NotifyController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly string _appId;
        private readonly string _appPassword;
        private readonly ConcurrentDictionary<string, ConversationReference> _conversationReferences;
        private readonly List<TeamsChannelAccount> _members;

        public NotifyController(IBotFrameworkHttpAdapter adapter, IConfiguration configuration, 
            ConcurrentDictionary<string, ConversationReference> conversationReferences,
            List<TeamsChannelAccount> members)
        {
            _adapter = adapter;
            _conversationReferences = conversationReferences;
            _appId = configuration["MicrosoftAppId"] ?? string.Empty;
            _appId = configuration["MicrosoftAppPassword"] ?? string.Empty;
            _members = members;
        }

        private bool IsEqual(TeamsChannelAccount member, string upn)
        {
            var result = member.UserPrincipalName.ToLower() == upn.ToLower();
            return result;
        }

        public async Task<ConversationReference> GetConversation(string upn)
        {
            ConversationReference conversationReference = null;

            TeamsChannelAccount member = _members.FirstOrDefault(m => IsEqual(m, upn));
            if (member == null)
            {
               return conversationReference;
            }

            _conversationReferences.TryGetValue(member.Id, out conversationReference);
            if (conversationReference == null)
            {
                conversationReference = await CreateConversationAsync(member);
                _conversationReferences.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
            }
            return conversationReference;
        }

        public async Task<IActionResult> PostAsync([FromBody] NotificationModel message, CancellationToken cancellationToken = default)
        {
            var conversationReference = await GetConversation(message.UPN);
            if (conversationReference != null)
            {
                await ((BotAdapter)_adapter).ContinueConversationAsync(_appId, conversationReference, BotCallback, default(CancellationToken));
            }
            
            // Let the caller know proactive messages have been sent
            return new ContentResult()
            {
                Content = "<html><body><h1>Proactive messages have been sent.</h1></body></html>",
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.OK,
            };
        }

        private async Task BotCallback(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync("proactive hello");
        }

        private async Task<ConversationReference> CreateConversationAsync(TeamsChannelAccount teamMember, CancellationToken cancellationToken = default)
        {
            var teamsChannelId = turnContext.Activity.TeamsGetChannelId();
            var serviceUrl = turnContext.Activity.ServiceUrl;
            var credentials = new MicrosoftAppCredentials(_appId, _appPassword);
            ConversationReference conversationReference = null;


            var proactiveMessage = MessageFactory.Text($"Hello {teamMember.GivenName} {teamMember.Surname}. I'm a Teams conversation bot.");

            var conversationParameters = new ConversationParameters
            {
                IsGroup = false,
                Bot = turnContext.Activity.Recipient,
                Members = isAadId ? new ChannelAccount[] { new ChannelAccount(teamMember.AadObjectId) } : new ChannelAccount[] { teamMember },
                TenantId = turnContext.Activity.Conversation.TenantId,
            };

            try
            {
                conversationReference = await((CloudAdapter)_adapter).CreateConversationAsync(
                credentials.MicrosoftAppId,
                teamsChannelId,
                serviceUrl,
                credentials.OAuthScope,
                conversationParameters,
                async (t1, c1) =>
                {
                    conversationReference = t1.Activity.GetConversationReference();
                    await ((CloudAdapter)turnContext.Adapter).ContinueConversationAsync(
                        _appId,
                        conversationReference,
                        async (t2, c2) =>
                        {
                            var message = await t2.SendActivityAsync(proactiveMessage, c2);
                            teamMemberDetails.TryAdd(teamMember.AadObjectId, teamMember);
                            teamMemberMessageIdDetails.TryAdd(teamMember.AadObjectId, message.Id);
                        },
                        cancellationToken);
                },
                cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return conversationReference;
        }
    }
}
