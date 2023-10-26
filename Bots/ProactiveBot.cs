// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;

namespace Microsoft.BotBuilderSamples
{
    public class ProactiveBot : ActivityHandler
    {
        // Message to send to users when the bot receives a Conversation Update event
        private const string WelcomeMessage = "Welcome to the Proactive Bot sample.  Navigate to http://localhost:3978/api/notify to proactively message everyone who has previously messaged this bot.";

        private ConcurrentDictionary<string, ConversationReference> _conversationReferences = new ConcurrentDictionary<string, ConversationReference>();
        private List<TeamsChannelAccount> _members; // = new List<TeamsChannelAccount>();
        private readonly string _appId;
        private readonly string _appPassword;
        private readonly string _tenantId;

        public ProactiveBot(IConfiguration configuration, List<TeamsChannelAccount> members)
        {
            _appId = configuration["MicrosoftAppId"] ?? string.Empty;
            _appPassword = configuration["MicrosoftAppPassword"] ?? string.Empty;
            _tenantId = configuration["MicrosoftAppTenantId"] ?? string.Empty;

            _members = members;
        }

        private void AddConversationReference(Activity activity)
        {
            var conversationReference = activity.GetConversationReference();
            _conversationReferences.AddOrUpdate(conversationReference.User.Id, conversationReference, (key, newValue) => conversationReference);
        }

        protected override Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            AddConversationReference(turnContext.Activity as Activity);

            return base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(WelcomeMessage), cancellationToken);
                }
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            AddConversationReference(turnContext.Activity as Activity);

            // Echo back what the user said
            await turnContext.SendActivityAsync(MessageFactory.Text($"You sent '{turnContext.Activity.Text}'"), cancellationToken);
        }

        protected override async Task OnInstallationUpdateActivityAsync(ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
             var botMember = new TeamsChannelAccount()
            {
                Id = turnContext.Activity.Recipient.Id,
                Name = turnContext.Activity.Recipient.Name
            };
            _members.Add(botMember);
            string continuationToken = null;

            do
            {
                // Gets a paginated list of members of one-on-one, group, or team conversation.
                var currentPage = await TeamsInfo.GetPagedMembersAsync(turnContext, 100, continuationToken, cancellationToken);
                continuationToken = currentPage.ContinuationToken;
                _members.AddRange(currentPage.Members);
            }
            while (continuationToken != null);
        }

        private bool IsEqual(TeamsChannelAccount member, string upn)
        {
            var result = member.UserPrincipalName.ToLower() == upn.ToLower();
            return result;
        }

        public async Task<TeamsChannelAccount> GetTeamMember(string upn)
        {

            TeamsChannelAccount member = _members.FirstOrDefault(m => IsEqual(m, upn));
            return member;
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

        private async Task<ConversationReference> CreateConversationAsync(TeamsChannelAccount teamMember, CancellationToken cancellationToken = default)
        {
            var teamsChannelId = teamMember.Id;
            var serviceUrl = "https://smba.trafficmanager.net/teams/";
            var credentials = new MicrosoftAppCredentials(_appId, _appPassword);
            ConversationReference conversationReference = null;


            var proactiveMessage = MessageFactory.Text($"Hello {teamMember.GivenName} {teamMember.Surname}. I'm a Teams conversation bot.");

            var connectorClient = new ConnectorClient(new Uri(serviceUrl));

            /*           var parameters = new ConversationParameters
                       {
                           Bot = new ChannelAccount(botId, botName),
                           Members = new ChannelAccount[] { new ChannelAccount(teamsChannelId) },
                           ChannelData = new TeamsChannelData
                           {
                               Tenant = new TenantInfo(_tenantId)
                           }
                       };

                       try
                       {
                           var conversationResource = await connectorClient.Conversations.CreateConversationAsync(parameters);
                           conversationResource.ActivityId

                           IMessageActivity message = null;

                           if (conversationResource != null)
                           {
                               message = Activity.CreateMessageActivity();
                               message.From = new ChannelAccount(botId, botName);
                               message.Conversation = new ConversationAccount(id: conversationResource.Id.ToString());
                               message.Text = Strings.Send1on1Prompt;
                           }

                           await connectorClient.Conversations.SendToConversationAsync((Activity)message);
                       }
                       catch (Exception ex)
                       {

                       } */

            /*            var conversationParameters = new ConversationParameters
                        {
                            IsGroup = false,
                            Bot = turnContext.Activity.Recipient,
                            Members = new ChannelAccount[] { teamMember },
                            TenantId = _tenantId,
                        };

                        try
                        {
                            conversationReference = await ((CloudAdapter)_adapter).CreateConversationAsync(
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
                        } */

            return conversationReference;
        }

    }

}

