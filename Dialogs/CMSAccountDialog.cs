using System;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using System.Threading.Tasks;
using Microsoft.Azure.Documents.Client;
using System.Configuration;
using Microsoft.Azure.Documents;

namespace LuisBot.Dialogs
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

    // This class implements the form of CMS Request, take all the required information from the user and validates it.

    [Serializable]
    public class CMSFormRequest
    {

        
        [Prompt("Please enter first and last name of the CMS Editor: ")]
        public string FullName;
        public string SupervisorName;
        [Prompt("Which of the following account do {FullName} use? {||}")]
        public AccountOptions? AccountOption;
        [Prompt("Please enter the email address: ")]
        [Pattern(@"^([a-zA-Z0-9]+[a-zA-Z0-9\.]*[a-zA-Z0-9]+)@((umassmed)\.(edu)|(umassmemorial)\.(org))$")]
        public string userEmail;
        [Pattern(@"^([a-zA-Z0-9]+[a-zA-Z0-9\.]*[a-zA-Z0-9]+)@((umassmed)\.(edu)|(umassmemorial)\.(org))$")]
        public string SupervisorEmail;
        [Optional]
        [Prompt("Please enter the phone number:\n This is an Optional field, you can type 'no'. ")]
        [Pattern(@"(<Undefined control sequence>\d)?\s*\d{3}(-|\s*)\d{4}")]
        public string PhoneNumber = "";
        public string WebAddress;
        public string Department;
        [Optional]
        [Prompt("Please enter extra notes, if any.:\n This is an Optional field, you can type 'no'. ")]
        public string ExtraNotes = "";

        public static IForm<CMSFormRequest> BuildForm()
        {
            

            return new FormBuilder<CMSFormRequest>()
                    .Field(nameof(FullName))
                    .Field(nameof(AccountOption))
                    .Field(nameof(userEmail))
                    .Field(new FieldReflector<CMSFormRequest>(nameof(Department))
                        .SetValidate(ValidateDepartment))
                    .Field(new FieldReflector<CMSFormRequest>(nameof(WebAddress))
                        .SetValidate(ValidateWebAddress))
                    .Field(nameof(PhoneNumber))
                    .AddRemainingFields()
                    .Confirm(async (state) => {
                        return new PromptAttribute($"Please confirm with Yes/No:\n- UserName: {state.FullName}\n- User Email: {state.userEmail} " +
                            $"\n- Department: {state.Department}\n- Web Address: {state.WebAddress}\n- Phone Number: {state.PhoneNumber}\n- Supervisor Name: {state.SupervisorName}" +
                            $"\n- Supervisor Email: {state.SupervisorEmail}\n- Extra Notes: {state.ExtraNotes}");
                    })
                    .Build();
        }


        public static async Task<ValidateResult> ValidateWebAddress(CMSFormRequest state, object value)
        {
            var asString = value as string;
            var result = new ValidateResult() { IsValid = false, Value = value };
            if (!string.IsNullOrEmpty(asString))
            {
                var extracted = ExtractWebAddress(asString);
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

        public static string ExtractWebAddress(string url)
        {
            bool isUri = Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute);

            if (!isUri)
            {
                url = null;
            }

            return url;
        }


        public static async Task<ValidateResult> ValidateDepartment(CMSFormRequest state, object value)
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
                if(dept == null)
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
    }
}