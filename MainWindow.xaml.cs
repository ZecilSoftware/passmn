using Azure.AI.OpenAI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using System.Net;

namespace Dalle3_CSharp_Advent
{
    public sealed partial class MainWindow : Window
    {
        private const string OPENAI_KEY = "";
        private const string SAVE_FOLER = "Holiday DALLE";
        private Uri _currentImage;
        private string _currentPrompt;

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            WorkingState();
            var folder = await GetPicturesFolder();
            var selectedHoliday = GetSelectedHoliday();
            var holidayFolder = Path.Combine(folder, SAVE_FOLER, selectedHoliday);
            Directory.CreateDirectory(holidayFolder);
            
            var destination = Path.Combine(holidayFolder, $"{HumanPrompt.Text}.png");
            var destination2 = Path.Combine(holidayFolder, $"{HumanPrompt.Text}.txt");

            using (var client = new WebClient())
            {
                client.DownloadFile(_currentImage, destination);
            }

            using (StreamWriter outputFile = new StreamWriter(destination2, false))
            {
                outputFile.WriteLine(_currentPrompt);
            }

            SaveNotification.Subtitle = destination;
            SaveNotification.IsOpen = true;
            FinishedState();
        }

        private async void GenerateImage_Click(object sender, RoutedEventArgs e)
        {
            GeneratedImage.Source = null;
            WorkingState();

            var selectedHoliday = GetSelectedHoliday();
            _currentPrompt = await GeneratePrompt(HumanPrompt.Text, selectedHoliday);

            ShowPrompt(_currentPrompt);
            var image = await GenerateImage(_currentPrompt);
            HidePrompt();

            GeneratedImage.Source = image;

            FinishedState();
            Save.IsEnabled = true;
        }

        private static async Task<string> GeneratePrompt(string userPrompt, string holiday)
        {
            OpenAIClient client = new(OPENAI_KEY);

            var systemMessage = GetHolidaySystemMessage(holiday);

            var responseCompletion = await client.GetChatCompletionsAsync(
                new ChatCompletionsOptions()
                {
                    ChoiceCount = 1,
                    Temperature = 1,
                    MaxTokens = 256,                    
                    DeploymentName = "gpt-4",
                    Messages = {
                        new ChatRequestSystemMessage(systemMessage),
                        new ChatRequestUserMessage(userPrompt),
                    },
                });

            return responseCompletion.Value.Choices[0].Message.Content;
        }

        private async Task<BitmapImage> GenerateImage(String prompt)
        {
            OpenAIClient client = new(OPENAI_KEY);

            var responseImages = await client.GetImageGenerationsAsync(
                new ImageGenerationOptions()
                {
                    ImageCount = 1,
                    Prompt = prompt,
                    Size = ImageSize.Size1792x1024,
                    DeploymentName = "dall-e-3"
                });

            _currentImage = responseImages.Value.Data[0].Url;
            return new BitmapImage(_currentImage);
        }

        private void ShowPrompt(string prompt)
        {
            GeneratedPrompt.Text = prompt;
            GeneratedPrompt.Visibility = Visibility.Visible;
        }

        private void HidePrompt()
        {
            GeneratedPrompt.Visibility = Visibility.Collapsed;
        }

        private void WorkingState()
        {
            Save.IsEnabled = false;
            Generate.IsEnabled = false;
            ProgressIndicator.IsActive = true;
        }

        private void FinishedState()
        {
            ProgressIndicator.IsActive = false;
            Generate.IsEnabled = true;
        }

        private static async Task<string> GetPicturesFolder()
        {
            var myPictures = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Pictures);
            Directory.CreateDirectory(Path.Combine(myPictures.SaveFolder.Path, SAVE_FOLER));
            return myPictures.SaveFolder.Path;
        }

        private string GetSelectedHoliday()
        {
            var selectedItem = HolidaySelector.SelectedItem as ComboBoxItem;
            return selectedItem?.Content?.ToString() ?? "Christmas";
        }

        private static string GetHolidaySystemMessage(string holiday)
        {
            return holiday switch
            {
                "Christmas" => "Create a prompt for Dall-e that will generate a beautiful Christmas scene using the following text for inspiration:",
                "Valentine's Day" => "Create a prompt for Dall-e that will generate a romantic Valentine's Day scene with hearts, roses, and love themes using the following text for inspiration:",
                "Easter" => "Create a prompt for Dall-e that will generate a festive Easter scene with spring flowers, Easter eggs, bunnies, and pastel colors using the following text for inspiration:",
                "Halloween" => "Create a prompt for Dall-e that will generate a spooky Halloween scene with pumpkins, ghosts, witches, and autumn themes using the following text for inspiration:",
                "Birthday" => "Create a prompt for Dall-e that will generate a celebratory birthday scene with cake, balloons, presents, and party decorations using the following text for inspiration:",
                _ => "Create a prompt for Dall-e that will generate a beautiful festive scene using the following text for inspiration:"
            };
        }
    }
}