using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.Bot.Sample.LuisBot;

namespace LuisBot.Dialogs
{
    public enum YesOrNo
    {
        [Describe("1. Yes")]
        Yes = 1,

        [Describe("2. No")]
        No = 2
    };

    public enum CampusOptions
    {
        [Describe("1. Office")]
        Office = 1,

        [Describe("2. OffCampus")]
        OffCampus = 2
    };

    public enum ConnectionOptions
    {
        [Describe("1. Wireless")]
        Wireless = 1,

        [Describe("2. LAN")]
        LAN = 2

    };

    [Serializable]
    public class LoginIssuesDialog : IDialog<object>
    {
        public YesOrNo? IsEditor;
        public CampusOptions? WhichCampus;
        public ConnectionOptions? ConnectionType;
        public string Fullname_lan;
        public string Department_lan;
        public string UserEmail_lan;
        public string ExtraNotes_lan;
        public YesOrNo? FurtherAssistance;
        public bool EmailToTeamForFurtherAssistance = false;
        

        public async Task StartAsync(IDialogContext context)
        {

            await context.PostAsync("I see, you are having issues logging in. Is that correct?.");
            context.Wait(MessageReceivedAsync);

        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> response)
        {
            var reply = await response;
            string temp = reply.Text;
            if(reply.Text.ToString().ToLower() == "yes")
            {
                PromptDialog.Choice(
                context: context,
                resume: AfterIsEditor,
                prompt: "Are you currently an editor for a particular website? ",
                options: (IEnumerable<YesOrNo>)Enum.GetValues(typeof(YesOrNo)),
                retry: "Please select one of the choices.",
                promptStyle: PromptStyle.Auto
                );
            }
            else
            {
                context.Done(true);
            }
        }


        public async Task AfterIsEditor(IDialogContext context, IAwaitable<YesOrNo> result)
        {
            YesOrNo response = await result;
            IsEditor = response;
            if(response.ToString().ToLower() == "no")
            {
                PromptDialog.Choice(
                   context: context,
                   resume: SendToCMSForm,
                   prompt: "If you are not an editor, you'll have to fill a form for CMS authorization. Would you like me to fill out the form for you?",
                   options: (IEnumerable<YesOrNo>)Enum.GetValues(typeof(YesOrNo)),
                   retry: "Please select one of the choices.",
                   promptStyle: PromptStyle.Auto
                   );
                
                
            }
            else
            {
                PromptDialog.Choice(
                context: context,
                resume: AfterWhichCampus,
                prompt: "Are you trying to login from the office or off campus? ",
                options: (IEnumerable<CampusOptions>)Enum.GetValues(typeof(CampusOptions)),
                retry: "Please select one of the choices.",
                promptStyle: PromptStyle.Auto
                );
            }
        }

        public async Task SendToCMSForm(IDialogContext context, IAwaitable<YesOrNo> result)
        {
            YesOrNo res = await result;
            if(res.ToString().ToLower() == "no")
            {
                PromptDialog.Choice(
                   context: context,
                   resume: AfterFurtherAssistance,
                   prompt: "Do you still require help with this issue? ",
                   options: (IEnumerable<YesOrNo>)Enum.GetValues(typeof(YesOrNo)),
                   retry: "Please select one of the choices.",
                   promptStyle: PromptStyle.Auto
                   );
            }
            else
            {
                var myform = new FormDialog<CMSFormRequest>(new CMSFormRequest(), CMSFormRequest.BuildForm, FormOptions.PromptInStart, null);
                context.Call(myform, AfterCMSFormComplete);
            }
        }
        public async Task AfterWhichCampus(IDialogContext context, IAwaitable<CampusOptions> result)
        {
            CampusOptions response = await result;
            WhichCampus = response;
            if (response.ToString().ToLower() == "offcampus")
            {
                await context.PostAsync("To remotely connect to the UMMS network to access the CMS," +
                    $"you will need to connect through a Virtual Private Network or VPN. The easiest way to do this is to go to https://remote.umassmed.edu. ");

                PromptDialog.Choice(
                context: context,
                resume: AfterFurtherAssistance,
                prompt: "Do you still require help with this issue? ",
                options: (IEnumerable<YesOrNo>)Enum.GetValues(typeof(YesOrNo)),
                retry: "Please select one of the choices.",
                promptStyle: PromptStyle.Auto
                );
            }
            else
            {
                PromptDialog.Choice(
                context: context,
                resume: AfterConnectionOptions,
                prompt: "Are you on connected to lan cable or wireless? ",
                options: (IEnumerable<ConnectionOptions>)Enum.GetValues(typeof(ConnectionOptions)),
                retry: "Please select one of the choices.",
                promptStyle: PromptStyle.Auto
                );
            }
        }

        public async Task AfterConnectionOptions(IDialogContext context, IAwaitable<ConnectionOptions> result)
        {
            ConnectionOptions response = await result;
            ConnectionType = response;
            if (response.ToString().ToLower() == "wireless")
            {
                await context.PostAsync(" Make sure you are connected to the UMMS network, other networks will not allow you to connect to the CMS. ");
                PromptDialog.Choice(
                context: context,
                resume: AfterFurtherAssistance,
                prompt: "Do you still require help with this issue? ",
                options: (IEnumerable<YesOrNo>)Enum.GetValues(typeof(YesOrNo)),
                retry: "Please select one of the choices.",
                promptStyle: PromptStyle.Auto
                );
            }
            else
            {
                PromptDialog.Text(
                context: context,
                resume: AfterGetName,
                prompt: "Let me gather some info and send an email to the web team so they can help you with this issue.\n Please enter your first and last name. "
                );
            }
        }

        public async Task AfterGetName(IDialogContext context, IAwaitable<string> result)
        {
            Fullname_lan = await result;
            PromptDialog.Text(
               context: context,
               resume: AfterGetEmail,
               prompt: "Please enter your email id. "
               );
        }

        public async Task AfterGetEmail(IDialogContext context, IAwaitable<string> result)
        {
            UserEmail_lan = await result;
            PromptDialog.Text(
               context: context,
               resume: AfterGetDepartment,
               prompt: "Please enter your department."
               );

        }
        
        public async Task AfterGetDepartment(IDialogContext context, IAwaitable<string> result)
        {
            Department_lan = await result;
            await SendEmail(context);
            context.Done(true);
        }

        public async Task SendEmail(IDialogContext context)
        {
            var smtpClient = new System.Net.Mail.SmtpClient();
            var msg = new System.Net.Mail.MailMessage();
            msg.To.Add("mahima.parashar@umassmed.edu");
            msg.Subject = "CMS Login Issues";
            if (EmailToTeamForFurtherAssistance == false)
            {
                msg.Body = @"{Fullname_lan} from {Department_lan} is unable to log into the CMS from their office computer.";
            }
            else
            {
                msg.Body = $"{Fullname_lan} from {Department_lan} is unable to log into the CMS from on/off campus and on wireless/LAN line.";
            }
            smtpClient.Send(msg);
            await context.PostAsync("An email has been sent to the web team. They will assist you shortly.");
            
        }

        public async Task AfterFurtherAssistance(IDialogContext context, IAwaitable<YesOrNo> result)
        {
            FurtherAssistance = await result;
            if(FurtherAssistance.ToString().ToLower() == "no")
            {
                await FormCompleted(context);
            }
            else
            {
                EmailToTeamForFurtherAssistance = true;
                if (Fullname_lan == "" || Fullname_lan == null)
                {
                    PromptDialog.Text(
                    context: context,
                    resume: AfterGetName,
                    prompt: "Let me gather some info and send an email to the web team so they can help you with this issue.\n Please enter your first and last name. "
                    );
                }
                else
                {
                    await SendEmail(context);
                    context.Done(true);
                }
            }
        }
        
        public async Task AskIfRequireHelp(IDialogContext context)
        {
            PromptDialog.Choice(
            context: context,
            resume: AfterFurtherAssistance,
            prompt: "Do you still require help with this issue? ",
            options: (IEnumerable<YesOrNo>)Enum.GetValues(typeof(YesOrNo)),
            retry: "Please select one of the choices.",
            promptStyle: PromptStyle.Auto
            );
        }
        public async Task FormCompleted(IDialogContext context)
        {
            context.Done(true);
        }

        public async Task AfterCMSFormComplete(IDialogContext context, IAwaitable<CMSFormRequest> result)
        {
            CMSFormRequest cmsRequest = null;

            try
            {
                cmsRequest = await result;
            }
            catch (OperationCanceledException)
            {
                await context.PostAsync("You Cancelled the form!");
                return;
            }

            if (cmsRequest != null)
            {

                context.PrivateConversationData.SetValue("ProfileComplete", true);
                context.PrivateConversationData.SetValue("FullName", cmsRequest.FullName);
                context.PrivateConversationData.SetValue("Department", cmsRequest.Department);
                context.PrivateConversationData.SetValue("UserEmail", cmsRequest.userEmail);
                context.PrivateConversationData.SetValue("WebAddress", cmsRequest.WebAddress);
                if (cmsRequest.PhoneNumber == null)
                {
                    context.PrivateConversationData.SetValue("Phone", "");
                }
                else
                {
                    context.PrivateConversationData.SetValue("Phone", cmsRequest.PhoneNumber);
                }

                if (cmsRequest.ExtraNotes == null)
                {
                    context.PrivateConversationData.SetValue("ExtraNotes", "");
                }
                else
                {
                    context.PrivateConversationData.SetValue("ExtraNotes", cmsRequest.ExtraNotes);
                }

                context.PrivateConversationData.SetValue("SupervisorName", cmsRequest.SupervisorName);
                context.PrivateConversationData.SetValue("SupervisorEmail", cmsRequest.SupervisorEmail);

                BasicLuisDialog.SendEmail(cmsRequest, null);

                await context.PostAsync("A request for CMS Account is successfuly raised. You will receive a confirmation email.");

                Fullname_lan = cmsRequest.FullName;
                Department_lan = cmsRequest.Department;
                UserEmail_lan = cmsRequest.userEmail;

                PromptDialog.Choice(
                context: context,
                resume: AfterFurtherAssistance,
                prompt: "Do you still require help with this issue? ",
                options: (IEnumerable<YesOrNo>)Enum.GetValues(typeof(YesOrNo)),
                retry: "Please select one of the choices.",
                promptStyle: PromptStyle.Auto
                );

            }
            else
            {
                await context.PostAsync("Form returned empty response!");
            }

        }

    }
}
