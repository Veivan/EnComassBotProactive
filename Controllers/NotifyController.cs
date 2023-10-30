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
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.BotBuilderSamples.Models;
using Microsoft.Extensions.Configuration;
using ProactiveBot.Services;

namespace Microsoft.BotBuilderSamples.Controllers
{
    [Route("api/notify")]
    [ApiController]
    public class NotifyController : ControllerBase
    {
        private readonly string _appId;
        private readonly string _appPassword;
        private readonly string _tenantId;
        // private List<TeamsChannelAccount> _members;
        private readonly string _botIDprefix;
        private readonly ITeamMemberService _teamMemberService;

        public NotifyController(IConfiguration configuration, ITeamMemberService teamMemberService)
        {
            _appId = configuration["MicrosoftAppId"] ?? string.Empty;
            _appPassword = configuration["MicrosoftAppPassword"] ?? string.Empty;
            _tenantId = configuration["MicrosoftAppTenantId"] ?? string.Empty;
            //_members = members;
            _botIDprefix = configuration["BotIDprefix"] ?? string.Empty; ;
            _teamMemberService = teamMemberService;
        }

        private bool IsEqual(TeamsChannelAccount member, string upn)
        {
            var result = member.UserPrincipalName?.ToLower() == upn.ToLower();
            return result;
        }

        public async Task<IActionResult> PostAsync([FromBody] NotificationModel inpMessage, CancellationToken cancellationToken = default)
        {
            var botMember = await _teamMemberService.FindTeamMemberAsync(_botIDprefix);
            if (botMember == null)
            {
                string mess = "Bot member not found";
                return FormatResult(mess);
            }

            var teamMember = await _teamMemberService.GetTeamMemberAsync(inpMessage.UPN);

            if (teamMember == null)
            {
                string mess = $"Team member {inpMessage.UPN} not found";
                return FormatResult(mess);
            }

            //TeamsChannelAccount teamMember = _members.FirstOrDefault(m => IsEqual(m, inpMessage.UPN));

            var serviceUrl = "https://smba.trafficmanager.net/teams/";
            var credentials = new MicrosoftAppCredentials(_appId, _appPassword);

            var connectorClient = new ConnectorClient(new Uri(serviceUrl), credentials);
            var botAcc = new ChannelAccount(botMember.Id, botMember.Name);

            var parameters = new ConversationParameters
            {
                Bot = botAcc,
                Members = new ChannelAccount[] { new ChannelAccount(teamMember.Id) },
                ChannelData = new TeamsChannelData
                {
                    Tenant = new TenantInfo(_tenantId)
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
                    Content = $"<html><body><h1>Proactive messages have been sent to {teamMember.Name}.</h1></body></html>",
                    ContentType = "text/html",
                    StatusCode = (int)HttpStatusCode.OK,
                };
            }
            catch (Exception ex)
            {
                return FormatResult(ex.Message);
            }
        }

        private ContentResult FormatResult(string message)
        {
            return new ContentResult()
            {
                Content = $"<html><body><h1>{message}</h1></body></html>",
                ContentType = "text/html",
                StatusCode = (int)HttpStatusCode.BadRequest,
            };
        }

    }
}
