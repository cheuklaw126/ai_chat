// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples
{
    // This bot will respond to the user's input with suggested actions.
    // Suggested actions enable your bot to present buttons that the user
    // can tap to provide input. 
    public class SuggestedActionsBot : ActivityHandler
    {
        public const string WelcomeText = "I am AI Chat Bot what can I help you";
        static string  currentuser = "";
      
        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            // Send a welcome message to the user and tell them what actions they may perform to use this bot
            await SendWelcomeMessageAsync(turnContext, cancellationToken);
        }
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            // Extract the text from the message activity the user sent.
            var text = turnContext.Activity.Text.ToLowerInvariant();

            // Take the input from the user and create the appropriate response.
            var responseText = ProcessInput(text);

            // Respond to the user.
            await turnContext.SendActivityAsync(responseText, cancellationToken: cancellationToken);

            await SendSuggestedActionsAsync(turnContext, cancellationToken);
        }
        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    currentuser = member.Name;
                    await turnContext.SendActivityAsync(
                        $"Halo {member.Name}, my name is Marry . {WelcomeText}",
                        cancellationToken: cancellationToken);
                    await SendSuggestedActionsAsync(turnContext, cancellationToken);
                }
            }
        }
        public static async Task<JObject> GetAI(string Query)
        {
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = "https://westus.api.cognitive.microsoft.com/luis/prediction/v3.0/apps/65a7ea40-ae05-4a9d-be8d-e30954fafc5f/slots/production/predict?subscription-key=9749987754fb4f4bb62763477e233f7e&query=" + Query;
                HttpResponseMessage msg = await client.GetAsync(RequestURI);

                if (msg.IsSuccessStatusCode)
                {
                    return   JObject.Parse(await msg.Content.ReadAsStringAsync());

                }
            }
            return new JObject();
        }

        public static async Task<JObject> SendEmail(string bdy)
        {
            using (HttpClient client = new HttpClient())
            {
                string RequestURI = $@"https://doubleslash.ltd/home/SendMail?hdr=[online asset management system chat info]&msg=this is message from chat 
{currentuser} leave a message of {bdy}
";
                HttpResponseMessage msg = await client.GetAsync(RequestURI);

                if (msg.IsSuccessStatusCode)
                {
                    return JObject.Parse(await msg.Content.ReadAsStringAsync());

                }
            }
            return new JObject();
        }

        private static string ProcessInput(string text)
        {

            var task2 = Task.Run(async () =>

            await GetAI(text)
            );
            var dd = task2.GetAwaiter().GetResult();
            var action = dd["prediction"]["topIntent"].ToString().ToLower();
            string adMsg = "";
            switch (action)
            {
                case "redirect":
                    var type = dd["prediction"]["entities"]["action"]?[0]?[0]?.ToString() ?? "";
                    var obj = dd["prediction"]["entities"]?["object"]?[0]?[0]?.ToString() ?? "";
                    if (type == "")
                        type = "index";
                    if(obj!="")

                    adMsg = $"you would like to visit https://doubleslash.ltd/{obj}/{type}";
                    else
                    {
                        adMsg = "but I dont where is it.";
                    }
                    break;



                case "contact":
                    var task3 = Task.Run(async () =>

           await SendEmail(text)
           );
                    var cc = task3.GetAwaiter().GetResult();


                    adMsg = $"I have forwarded your meassge to admin by email {cc["result"]}";
                    break;

               
            }
            return $"I guess you are thinking {action}, {adMsg}";

        }

        // Creates and sends an activity with suggested actions to the user. When the user
        /// clicks one of the buttons the text value from the "CardAction" will be
        /// displayed in the channel just as if the user entered the text. There are multiple
        /// "ActionTypes" that may be used for different situations.
        private static async Task SendSuggestedActionsAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text("What do you want?");

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                 
                    //new CardAction() { Title = "MaIl admin", Type = ActionTypes.ImBack, Value = "email", Image = "https://via.placeholder.com/20/0000FF?text=B", ImageAltText = "B"   },
                },
            };
            await turnContext.SendActivityAsync(reply, cancellationToken);
        }
    }
}
