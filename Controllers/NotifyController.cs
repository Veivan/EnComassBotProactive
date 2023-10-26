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
using Microsoft.Bot.Connector;
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
        private readonly IBot _bot;
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly string _appId;
        private readonly string _appPassword;
        private List<TeamsChannelAccount> _members; // = new List<TeamsChannelAccount>();

        public NotifyController(IBotFrameworkHttpAdapter adapter, IConfiguration configuration, IBot bot, List<TeamsChannelAccount> members)
        {
            _adapter = adapter;
            _bot = bot;
            _appId = configuration["MicrosoftAppId"] ?? string.Empty;
            _appPassword = configuration["MicrosoftAppPassword"] ?? string.Empty;
            _members = members;
        }

        private bool IsEqual(TeamsChannelAccount member, string upn)
        {
            var result = member.UserPrincipalName?.ToLower() == upn.ToLower();
            return result;
        }

        public async Task<IActionResult> PostAsync([FromBody] NotificationModel inpMessage, CancellationToken cancellationToken = default)
        {
            var botId = _members.First().Id;
            var botName = _members.First().Name;

            TeamsChannelAccount teamMember = _members.FirstOrDefault(m => IsEqual(m, inpMessage.UPN));

            var serviceUrl = "https://smba.trafficmanager.net/teams/";
            var credentials = new MicrosoftAppCredentials(_appId, _appPassword);

            var connectorClient = new ConnectorClient(new Uri(serviceUrl), credentials);
            var botAcc = new ChannelAccount(botId, botName);
            var userAcc = new ChannelAccount(teamMember.Id, teamMember.UserPrincipalName);

            var parameters = new ConversationParameters
            {
                Bot = botAcc,
                Members = new ChannelAccount[] { new ChannelAccount(teamMember.Id) },
                ChannelData = new TeamsChannelData
                {
                    Tenant = new TenantInfo(teamMember.TenantId)
                }
            };

            try
            {
                var conversationResource = await connectorClient.Conversations.CreateConversationAsync(parameters);

                IMessageActivity message = null;

                if (conversationResource != null)
                {
                    message = Activity.CreateMessageActivity();
                    message.From = botAcc;
                    message.Conversation = new ConversationAccount(id: conversationResource.Id.ToString());
                    message.Text = inpMessage.NotificationText;
                }

                if (inpMessage.Send)
                {
                    await connectorClient.Conversations.SendToConversationAsync((Activity)message);
                }

                // Let the caller know proactive messages have been sent
                return new ContentResult()
                {
                    Content = $"<html><body><h1>Proactive messages have been sent to {teamMember.UserPrincipalName}.</h1></body></html>",
                    ContentType = "text/html",
                    StatusCode = (int)HttpStatusCode.OK,
                };
            }
            catch (Exception ex)
            {
                return new ContentResult()
                {
                    Content = $"<html><body><h1>{ex.Message}</h1></body></html>",
                    ContentType = "text/html",
                    StatusCode = (int)HttpStatusCode.BadRequest,
                };
            }
        }
    }
}
