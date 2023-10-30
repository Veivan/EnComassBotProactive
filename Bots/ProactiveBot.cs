using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using ProactiveBot.Services;
using ProactiveBot.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples
{
    public class ProactiveBot : ActivityHandler
    {
        // Message to send to users when the bot receives a Conversation Update event
        private const string WelcomeMessage = "Welcome to the EncCompass Proactive Bot.";

        //  Navigate to http://localhost:3978/api/notify to proactively message everyone who has previously messaged this bot.        

        private readonly ITeamMemberService _teamMemberService;

        private readonly string _botIDprefix;

        public ProactiveBot(IConfiguration configuration, ITeamMemberService teamMemberService)
        {
            _botIDprefix = configuration["BotIDprefix"] ?? string.Empty; ;
            _teamMemberService = teamMemberService;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            //foreach (var member in membersAdded)
            //{
            //    // Greet anyone that was not the target (recipient) of this message.
            //    if (member.Id != turnContext.Activity.Recipient.Id)
            //    {
            //        await turnContext.SendActivityAsync(MessageFactory.Text(WelcomeMessage), cancellationToken);
            //    }
            //}

            await _teamMemberService.AddTeamMemberListAsync((IList<TeamMemberInfo>)membersAdded.Select(x => new TeamMemberInfo()
            {
                Id = x.Id,
                Name = x.Name
            }));
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // Echo back what the user said
            await turnContext.SendActivityAsync(MessageFactory.Text($"You sent '{turnContext.Activity.Text}'"), cancellationToken);
        }

        protected override async Task OnInstallationUpdateActivityAsync(ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var channels = new List<TeamsChannelAccount>();
            
            string continuationToken = null;

            do
            {
                // Gets a paginated list of members of one-on-one, group, or team conversation.
                var currentPage = await TeamsInfo.GetPagedMembersAsync(turnContext, 100, continuationToken, cancellationToken);
                continuationToken = currentPage.ContinuationToken;
                channels.AddRange(currentPage.Members);
            }
            while (continuationToken != null);

            List<TeamMemberInfo> members = channels.Select(x => new TeamMemberInfo()
            { Id = x.Id, Name = x.UserPrincipalName }).ToList();

            var botMember = new TeamMemberInfo()
            {
                Id = _botIDprefix + turnContext.Activity.Recipient.Id,
                Name = turnContext.Activity.Recipient.Name
            };
            members.Add(botMember);

            await _teamMemberService.AddTeamMemberListAsync(members);

        }
    }

}

