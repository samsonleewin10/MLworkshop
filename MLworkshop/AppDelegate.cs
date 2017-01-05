using Foundation;
using UIKit;
using MonoTouch.Dialog;
using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;

namespace MLworkshop
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to application events from iOS.
	[Register("AppDelegate")]
	public class AppDelegate : UIApplicationDelegate
	{
		UIWindow _window;
		UINavigationController _nav;
		DialogViewController _rootVC;
		RootElement _rootElement;

		public override bool FinishedLaunching(UIApplication app, NSDictionary options)
		{
			_window = new UIWindow(UIScreen.MainScreen.Bounds);

			_rootElement = new RootElement("Web services"){
				new Section ("Select Service"){
					 (Element)JsonElement.FromFile ("MLworkshop.json",this)
                     //You can add more JsonElements here by adding more json files which will enable more services
                }
			};

			_rootVC = new DialogViewController(_rootElement);
			_nav = new UINavigationController(_rootVC);

			_window.RootViewController = _nav;
			_window.MakeKeyAndVisible();

			return true;
		}
		//This method is invoked by the ontap method of "Submit Request".
		//It will collect the input and call a method to submit to the API.
		static void Callback(object obj)
		{
			AppDelegate apdel = (AppDelegate)obj;
			RootElement root = apdel._rootElement;

			List<string> colNames = new List<string>();
			List<string> colVals = new List<string>();
			int i = 0;

			foreach (Section s in root)
			{
				if (s.Elements.Count > 0)
				{
					foreach (JsonElement je in s.Elements)
					{
						foreach (Section s2 in je)
						{
							foreach (Element e2 in s2)
							{
								//Get user input data
								if (e2.GetType() == typeof(EntryElement))
								{
									try
									{
										EntryElement input = (EntryElement)e2;
										//Get input name and value and add to arrays
										colNames.Add(input.Caption);
										colVals.Add(input.Value);
										i++;
									}
									catch (Exception e)
									{
										string msg = e.Message.ToString();
										Console.WriteLine(msg);
									}

								}
							}
						}
					}
				}
			}
			//Submit the request
			InvokeRequestResponseService(colNames, colVals);
		}
		//Send API request to Azure ML RRS API
		static async void InvokeRequestResponseService(List<string> colNames, List<string> colValues)
		{
			const string apiKey = "YDaO2fjCiidryIfpE4N2JkweN1A1EfViWVD+6jAg2llyhn9MVJ3WhQjiO1CSFN0yhpIEGL+P3hUD1EunTAy9Sg=="; // Replace this with the API key for the web service
			const string apiUrl = "https://ussouthcentral.services.azureml.net/workspaces/f5e8e9bc4eed4034b78567449cfca779/services/278f86a1e8b4421b8b47a1c618805e2b/execute?api-version=2.0&details=true";

			//Column names and values
			StringTable stringTable = new StringTable();
			stringTable.ColumnNames = colNames.ToArray();

			int i = 0;
			int rowCnt = 1;//only a single row of input
			stringTable.Values = new string[rowCnt, colValues.Count];
			foreach (string item in colValues)
			{
				stringTable.Values[0, i] = item;
				i++;
			}
			//call the API
			using (var client = new HttpClient())
			{
				var scoreRequest = new
				{
					Inputs = new Dictionary<string, StringTable>() {
						{
							"input1",
							stringTable
						},
					},
					GlobalParameters = new Dictionary<string, string>()
					{ }
				};
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
				var uri = new Uri(apiUrl);

				var json = JsonConvert.SerializeObject(scoreRequest);
				var content = new StringContent(json, Encoding.UTF8, "application/json");

				HttpResponseMessage response = await client.PostAsync(uri, content);
				if (response.IsSuccessStatusCode)
				{
					//Get the result
					string result = await response.Content.ReadAsStringAsync();
					//Show it to user
					UIAlertView popup = new UIAlertView("Response", result, null, "Ok", null);
					popup.Show();
				}
				else
				{
					//Get response header info: includes the requert ID and the timestamp, which are useful for debugging the failure
					string responseHeader = response.Headers.ToString();
					//Get the content
					string responseContent = await response.Content.ReadAsStringAsync();
					//Show the error
					UIAlertView popup = new UIAlertView("Error", response.StatusCode + ": " + responseHeader + " " + responseContent, null, "Ok", null);
					popup.Show();
				}
			}
		}
	}
	public class StringTable
	{
		public string[] ColumnNames { get; set; }
		public string[,] Values { get; set; }
	}
}