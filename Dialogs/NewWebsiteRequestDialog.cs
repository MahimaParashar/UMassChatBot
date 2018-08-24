using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace LuisBot.Dialogs

    // This class implements the New Website request form, takes input from the user and validates it.
{
    [Serializable]
    public class NewWebsiteRequest
    {
        public enum AccountOptions
        {
            [Describe("1. UMass Medical")]
            umassMedical = 1,

            [Describe("2. UMass Memorial")]
            umassMemorial = 2
        };

        public enum YesOrNoOptions
        {
            [Describe("1. Yes")]
            Yes = 1,

            [Describe("2. No")]
            No = 2
        };

        [Prompt("Please enter first and last name: ")]
        public string FullName;
        [Prompt("Which of the following account do {FullName} use? {||}")]
        public AccountOptions? AccountOption;
        [Prompt("Enter your {AccountOption} email ID: ")]
        [Pattern(@"^([a-zA-Z0-9]+[a-zA-Z0-9\.]*[a-zA-Z0-9]+)@((umassmed)\.(edu)|(umassmemorial)\.(org))$")]
        public string userEmail;
        [Pattern(@"(<Undefined control sequence>\d)?\s*\d{3}(-|\s*)\d{4}")]
        public string PhoneNumber;
        public string WebAddress;
        public string Department;
        public string SupervisorName;
        [Pattern(@"^([a-zA-Z0-9]+[a-zA-Z0-9\.]*[a-zA-Z0-9]+)@((umassmed)\.(edu)|(umassmemorial)\.(org))$")]
        public string SupervisorEmail;
        [Optional]
        [Prompt(@"Please specify functionalities in the requested website if any.\n This is an Optional field, you can type 'no'.")]
        public string ExtraNotes;
        [Prompt("Please enter a target date (MM/DD/YYYY)")]
        public DateTime TargetDate;
        [Prompt(@"Do you need CMS Permissions?")]
        public YesOrNoOptions? NeedCMSPermissions;
        public YesOrNoOptions? PermissionForYourself;

        public static IForm<NewWebsiteRequest> BuildForm()
        {
            return new FormBuilder<NewWebsiteRequest>()
                    .Message("For setting up New Website Request, I would need the following information: ")
                    .Field(nameof(FullName))
                    .Field(nameof(AccountOption))
                    .Field(nameof(userEmail))
                    .Field(new FieldReflector<NewWebsiteRequest>(nameof(Department))
                        .SetValidate(ValidateDepartmentName))
                    .Field(new FieldReflector<NewWebsiteRequest>(nameof(WebAddress))
                        .SetValidate(ValidateWebAddress))
                    .Field(nameof(PhoneNumber))
                    .Field(nameof(TargetDate))
                    .Field(nameof(NeedCMSPermissions))
                    .Field(new FieldReflector<NewWebsiteRequest>(nameof(PermissionForYourself))
                        .SetActive((state) => state.NeedCMSPermissions.ToString().ToLower() == "yes")
                        .SetPrompt(new PromptAttribute("Do you need permissions for yourself?")))
                    .Field(new FieldReflector<NewWebsiteRequest>(nameof(SupervisorName))
                        .SetActive((state) => state.PermissionForYourself.ToString().ToLower() == "yes"))
                    .Field(new FieldReflector<NewWebsiteRequest>(nameof(SupervisorEmail))
                        .SetActive((state) => state.PermissionForYourself.ToString().ToLower() == "yes"))
                    .Confirm(async (state) =>
                    {
                        return new PromptAttribute($"Please confirm with Yes/No:\n- UserName: {state.FullName}\n- User Email: {state.userEmail}\n- Department: {state.Department}\n- Web Address: {state.WebAddress}\n- Phone Number: {state.PhoneNumber}" +
                            $"\n- Extra Notes: {state.ExtraNotes}");
                    })
                    .Build();
        }

        public static async Task<ValidateResult> ValidateDepartmentName(NewWebsiteRequest state, object value)
        {
            var comparisonString = value as string;
            var result = new ValidateResult() { IsValid = true, Value = value };

            string uri = "https://departments.documents.azure.com:443/";
            string key = ConfigurationManager.AppSettings["DocumentDbKey"];
            DocumentClient client;

            SqlQuerySpec query = new SqlQuerySpec("SELECT d.longname FROM departments d WHERE d.url = @comparisonString OR d.longname = @comparisonString");
            query.Parameters = new SqlParameterCollection();
            query.Parameters.Add(new SqlParameter("@comparisonString", comparisonString.ToLower()));

            client = new DocumentClient(new Uri(uri), key);

            foreach (var dept in client.CreateDocumentQuery(UriFactory.CreateDocumentCollectionUri("departments", "departments"), query))
            {
                if (dept == null)
                {
                    result.Value = comparisonString;
                }
                else
                {
                    result.Value = dept.longname;
                    return result;
                }
            }

            return result;

        }


        public static async Task<ValidateResult> ValidateWebAddress(NewWebsiteRequest state, object value)
        {
            var asString = value as string;
            var result = new ValidateResult() { IsValid = false, Value = value };
            if (!string.IsNullOrEmpty(asString))
            {
                var extracted = CMSFormRequest.ExtractWebAddress(asString);
                if (string.IsNullOrEmpty(extracted))
                {
                    result.Value = string.Empty;
                    result.Feedback = "That is not a valid web Address.  Please enter a valid URL.";
                }
                else
                {
                    result.Value = extracted;
                    result.IsValid = true;
                }
            }

            return result;
        }
    }
}