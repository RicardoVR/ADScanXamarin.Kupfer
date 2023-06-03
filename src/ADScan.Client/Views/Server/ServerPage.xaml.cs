using Newtonsoft.Json;
using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
using Xamarin.Forms;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using ADScan.Client.Models;
using ADScan.Client.Data;
using ADScan.Client.Renderers;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xamarin.Essentials;

namespace ADScan.Client.Views.Server
    {	
	    public partial class ServerPage : ContentPage
	    {

        ADScanDatabase database = null;
        public ServerPage ()
		    {
			    InitializeComponent ();
                txtServer.TextChanged += TxtServer_TextChanged;
                txtServer.Text = "95.111.235.29";
            btnAdd.Clicked += BtnAddressServer_Clicked;
            }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            if (database == null) {
                database = await ADScanDatabase.Instance;
            }
               
        }


        private void TxtServer_TextChanged(object sender, TextChangedEventArgs e)
            {
           
            }

            private async  void BtnAddressServer_Clicked(object sender, EventArgs e)
            {
           // txtLength.Text = "1";
            ai.IsRunning = true;
            btnAdd.IsVisible = false;


            var data = await database.GetAll<DeviceMessageList>();

            if (data.Count == 0)
            {
                Acr.UserDialogs.UserDialogs.Instance.Toast("Sin datos para enviar!");
                return;
            }

            await database.ClearMessagessServer();
            var index = 0;
            var exportRows = new List<DataRow>();
     

            //var data = await database.GetAll<DeviceMessageList>();

            //if (data.Count == 0)
            //{
            //    Acr.UserDialogs.UserDialogs.Instance.Toast("Sin datos para enviar!");
            //    ai.IsRunning = false;
            //    btnAdd.IsVisible = true;
            //    return;
            //}

            //  btnAdd.IsEnabled = false;
            var url = "http://95.111.235.29/conflux/api/externallist";
            string fileName = "LocalData.csv";
            string filePath = DependencyService.Get<IFileAccessHelper>().GetLocalFilePath(fileName);



            string[] columns = { "Id", "Created On", "Device", "MacAddress", "Raw" };

            string separator = ",";
            StringBuilder output = new StringBuilder();

            output.AppendLine(string.Join(separator, columns));

            var previousRaw = "";
            var dataList = new List<string>();
            try
            {

                foreach (var device in data)
                {

                    var deviceRow = ParseMessage(device.MacAddress, device.Raw);
                    var httpRequest = (HttpWebRequest)WebRequest.Create(url);
                    httpRequest.Method = "POST";

                    httpRequest.ContentType = "application/json";

                    var deviceData = new
                    {
                        Date = device.CreatedOn.ToString("dd/MM/yyyy"),
                        Hour = device.CreatedOn.ToString("HH:mm:ss"),
                        mac = device.MacAddress,
                        tipo = 10,
                        cont = int.Parse(deviceRow.Number),
                        adc1 = deviceRow.V1,
                        adc2 = deviceRow.V2,
                        desg = deviceRow.Desg,
                        Listened = 0,
                        bate = 235,
                        temp = 20,

                    };

                    string deviceJson = JsonConvert.SerializeObject(deviceData);
                    var parseObj = JObject.Parse(deviceJson);
                    JArray parseArray = new JArray(parseObj);

                    var serverData = new
                    {
                        type = @"sensor_batch",
                        data = parseArray,
                    };


                    //Tranform it to Json object

                    string finalData = JsonConvert.SerializeObject(serverData);
                    using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                    {
                        streamWriter.Write(finalData);
                    }

                    var httpResponse = (HttpWebResponse)httpRequest.GetResponse();

                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();
                    }
                    //index++;
                    //await Task.Delay(500);
                    //txtLength.Text = "sending" + index + "/" + data.Count;
                     exportRows.Add(
                        new DataRow()
                        {
                            Adc1 = deviceRow.V1,
                            Adc2 = deviceRow.V2,
                            Bate = "",
                            Cont = int.Parse(deviceRow.Number),
                            Date = device.CreatedOn.ToString("dd/MM/yyyy"),
                            Desg = deviceRow.Desg,
                            Hour = device.CreatedOn.ToString("HH:mm:ss"),
                            Listened = 0,
                            Mac = device.MacAddress,
                            Temp = 0,
                            Type = 10
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            try
            {

                foreach (var row in exportRows)
                {
                    index++;
                    await Task.Delay(500);
                    txtLength.Text = "sending" + index + "/" + exportRows.Count;
                }
                ai.IsRunning = false;
                btnAdd.IsVisible = true;
                if (index == exportRows.Count)
                {
                    txtLength.Text = "";
                }
                //  foreach (var device in data)
                //  {
                //      if (device.Raw == previousRaw || dataList.Contains($"{device.MacAddress}-{device.CreatedOn.ToString("ddMMyyyyHHmmss")}-{device.Raw}"))
                //      {
                //          continue;
                //      }

                //      // Generate csv line
                //      string[] newLine =
                //          {
                //                  device.ID.ToString(),
                //                  device.CreatedOn.ToString("yyyy/MM/dd HH:mm:ss"),
                //                  device.MacAddress,
                //                  device.Raw
                //          };

                //      output.AppendLine(string.Join(separator, newLine));

                //      // Generate line to send to web server
                //      var deviceRow = ParseMessage(device.MacAddress, device.Raw);

                //      exportRows.Add(
                //          new DataRow()
                //          {
                //              Adc1 = deviceRow.V1,
                //              Adc2 = deviceRow.V2,
                //              Bate = "",
                //              Cont = int.Parse(deviceRow.Number),
                //              Date = device.CreatedOn.ToString("dd/MM/yyyy"),
                //              Desg = deviceRow.Desg,
                //              Hour = device.CreatedOn.ToString("HH:mm:ss"),
                //              Listened = 0,
                //              Mac = device.MacAddress,
                //              Temp = 0,
                //              Type = 10
                //          }
                //      );

                //      previousRaw = device.Raw;
                //      dataList.Add($"{device.MacAddress}-{device.CreatedOn.ToString("ddMMyyyyHHmmss")}-{device.Raw}");
                //  }


                //  foreach (var row in exportRows)
                //  {
                //      index++;
                //      await Task.Delay(500);
                //      txtLength.Text = "sending" + index + "/" + exportRows.Count;
                //  }
                ////  await Task.Delay(3000);
                //  ai.IsRunning = false;
                //  btnAdd.IsVisible = true;
                //  if (index == exportRows.Count)
                //  {
                //      txtLength.Text = "";
                //  }

            }
            catch (Exception ex)
            {
                
                //  Acr.UserDialogs.UserDialogs.Instance.Alert("Error: " + ex.Message);
            }





   
            //if (index == data.Count)
            //{
            //    txtLength.Text = "";
            //    await database.ClearMessagessServer();
            //}



        }




        private Models.Device ParseMessage(string deviceAddress, string rawMessage)
        {

            // Si es 10 (HEX) 16 (DEC) es medidor de espesor (esparrago 250 mm)
            // Si es 11 (HEX) 16 (DEC) es medidor de espesor (esparrago 200 mm )
            // Si es 12 (HEX) 16 (DEC) es medidor de espesor (Perno 120 mm )

            try
            {
                // Contador ADV
                int ContadorIndex = ((9 * 2) - 2);
                String ContadorHex = rawMessage.Substring(ContadorIndex, 4);
                var ContadorValue = ParseHex(ContadorHex); // = Int16.Parse(pBarHex);

                var Contador = ContadorValue.ToString();

                // ADC_1
                int ADC_1_Index = ((11 * 2) - 2);
                string ADC_1_Hex = rawMessage.Substring(ADC_1_Index, 4);
                int ADC_1_Value = ParseHex(ADC_1_Hex);

                var ADC_1 = ADC_1_Value.ToString();

                // ADC_2
                int ADC_2_Index = ((13 * 2) - 2);
                string ADC_2_Hex = rawMessage.Substring(ADC_2_Index, 4);
                int ADC_2_Value = ParseHex(ADC_2_Hex);

                var ADC_2 = ADC_2_Value.ToString();

                // Status
                int statusIndex = ((11 * 2) - 2);
                String statusHex = rawMessage.Substring(statusIndex, 4);
                int statusValue = ParseHex(statusHex);
                string statusText = "";

                //statusText = "Revisar";

                int DesgasteIndex = ((21 * 2) - 2);
                String DesgasteHex = rawMessage.Substring(DesgasteIndex, 2);
                int DesgasteValue = ParseHex(DesgasteHex); //Integer.parseInt(DesgasteHex, 16);

                if (DesgasteValue >= 254)
                { statusText = "Revisar"; }
                else
                {
                    statusText = DesgasteValue.ToString() + "mm";
                }

                if (ADC_1_Value <= 147 && ADC_2_Value <= 147)
                {
                    statusText = "Sin Sensor";
                }
                if (ADC_1_Value > 147 && ADC_2_Value <= 147)
                {
                    statusText = "Mal Conectado";
                }
                if (ADC_1_Value <= 147 && ADC_2_Value > 147)
                {
                    statusText = "Mal Conectado";
                }

                var status = statusText;

                return new Models.Device()
                {
                    Mac = deviceAddress,
                    Number = Contador,
                    V1 = ADC_1,
                    V2 = ADC_2,
                    MM = status,
                    Desg = DesgasteValue,
                    RAW_ADV = rawMessage
                };
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private int ParseHex(string hex)
        {
            return int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
        }

    }
    }

