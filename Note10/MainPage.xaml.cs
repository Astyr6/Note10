﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel.Channels;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Pour plus d'informations sur le modèle d'élément Page vierge, consultez la page https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Note10
{
    /// <summary>
    /// Une page vide peut être utilisée seule ou constituer une page de destination au sein d'un frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private Color currentColor = Colors.Black;
        private string LastFormattedText = "";
        private int LastRawTextLength = 0;

        private async void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            // Open a text file.
            Windows.Storage.Pickers.FileOpenPicker open =
                new Windows.Storage.Pickers.FileOpenPicker();
            open.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            open.FileTypeFilter.Add(".rtf");
            open.FileTypeFilter.Add(".txt");

            Windows.Storage.StorageFile file = await open.PickSingleFileAsync();

            if (file != null)
            {
                using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                    await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                {
                    // Load the file into the Document property of the RichEditBox.
                    editor.Document.LoadFromStream(Windows.UI.Text.TextSetOptions.FormatRtf, randAccStream);
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            FileSavePicker savePicker = new FileSavePicker();
            savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

            // Dropdown of file types the user can save the file as
            savePicker.FileTypeChoices.Add("Rich Text", new List<string>() { ".rtf" });
            savePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt" });

            // Default file name if the user does not type one in or select a file to replace
            savePicker.SuggestedFileName = "NewDocument";

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                // Prevent updates to the remote version of the file until we 
                // finish making changes and call CompleteUpdatesAsync.
                CachedFileManager.DeferUpdates(file);
                // write to file
                if (file.FileType == ".rtf")
                {
                    using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                        await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                    {
                        editor.Document.SaveToStream(Windows.UI.Text.TextGetOptions.FormatRtf, randAccStream);
                    }
                }

                if (file.FileType == ".txt")
                {
                    using (Windows.Storage.Streams.IRandomAccessStream randAccStream =
                        await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                    {
                        editor.Document.SaveToStream(Windows.UI.Text.TextGetOptions.None, randAccStream);
                    }
                }

                // Let Windows know that we're finished changing the file so the 
                // other app can update the remote version of the file.
                FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                if (status != FileUpdateStatus.Complete)
                {
                    Windows.UI.Popups.MessageDialog errorBox =
                        new Windows.UI.Popups.MessageDialog("File " + file.Name + " couldn't be saved.");
                    await errorBox.ShowAsync();
                }
            }
        }

        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.CharacterFormat.Bold = FormatEffect.Toggle;
        }

        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            editor.Document.Selection.CharacterFormat.Italic = FormatEffect.Toggle;
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract the color of the button that was clicked.
            Button clickedColor = (Button)sender;
            var rectangle = (Windows.UI.Xaml.Shapes.Rectangle)clickedColor.Content;
            var color = ((Windows.UI.Xaml.Media.SolidColorBrush)rectangle.Fill).Color;

            editor.Document.Selection.CharacterFormat.ForegroundColor = color;

            fontColorButton.Flyout.Hide();
            editor.Focus(Windows.UI.Xaml.FocusState.Keyboard);
            currentColor = color;
        }

        private void FindBoxHighlightMatches()
        {
            FindBoxRemoveHighlights();

            Color highlightBackgroundColor = (Color)App.Current.Resources["SystemColorHighlightColor"];
            Color highlightForegroundColor = (Color)App.Current.Resources["SystemColorHighlightTextColor"];

            string textToFind = findBox.Text;
            if (textToFind != null)
            {
                ITextRange searchRange = editor.Document.GetRange(0, 0);
                while (searchRange.FindText(textToFind, TextConstants.MaxUnitCount, FindOptions.None) > 0)
                {
                    searchRange.CharacterFormat.BackgroundColor = highlightBackgroundColor;
                    searchRange.CharacterFormat.ForegroundColor = highlightForegroundColor;
                }
            }
        }

        private void FindBoxRemoveHighlights()
        {
            ITextRange documentRange = editor.Document.GetRange(0, TextConstants.MaxUnitCount);
            SolidColorBrush defaultBackground = editor.Background as SolidColorBrush;
            SolidColorBrush defaultForeground = editor.Foreground as SolidColorBrush;

            documentRange.CharacterFormat.BackgroundColor = defaultBackground.Color;
            documentRange.CharacterFormat.ForegroundColor = defaultForeground.Color;
        }

        private void Editor_GotFocus(object sender, RoutedEventArgs e)
        {
            editor.Document.GetText(TextGetOptions.UseCrlf, out string currentRawText);
            if (currentRawText.Length != LastRawTextLength)
            {
                // User used cut or paste from action command, skip the event
                return;
            }
            // reset colors to correct defaults for Focused state
            ITextRange documentRange = editor.Document.GetRange(0, TextConstants.MaxUnitCount);
            //SolidColorBrush background = (SolidColorBrush)App.Current.Resources["TextControlBackgroundFocused"];
            SolidColorBrush foreground = (SolidColorBrush)App.Current.Resources["TextControlForegroundFocused"];

            editor.Document.ApplyDisplayUpdates();

            if (/*background != null &&*/ foreground != null)
            {
                //documentRange.CharacterFormat.BackgroundColor = background.Color;
            }
            // saving selection span
            var caretPosition = editor.Document.Selection.GetIndex(TextRangeUnit.Character) - 1;
            if (caretPosition <= 0)
            {
                caretPosition = 1;
            }
            var selectionLength = editor.Document.Selection.Length;
            // restoring text styling, unintentionally sets caret position at beginning of text
            editor.Document.SetText(TextSetOptions.FormatRtf, LastFormattedText);
            // restoring selection position
            editor.Document.Selection.SetIndex(TextRangeUnit.Character, caretPosition, false);
            editor.Document.Selection.SetRange(caretPosition, caretPosition + selectionLength);
            // Editor might have gained focus because user changed color.
            // Change selection color
            // Note that only way to regain with selection containing text is using the change color button
            editor.Document.Selection.CharacterFormat.ForegroundColor = currentColor;
        }

        private void Editor_LosingFocus(object sender, RoutedEventArgs e)
        {
            // Save text length to determine text length change
            editor.Document.GetText(TextGetOptions.UseCrlf, out string lastRawText);
            LastRawTextLength = lastRawText.Length;

            // Save formatted to restore formatting upon regaining focus
            editor.Document.GetText(TextGetOptions.FormatRtf, out LastFormattedText);
        }

        private void Editor_TextChanging(object sender, RichEditBoxTextChangingEventArgs e)
        {
            // Fix bug where selected text would get colored when editor loses focus
            if (FocusManager.GetFocusedElement() == editor)
            {
                editor.Document.Selection.CharacterFormat.ForegroundColor = currentColor;
            }
        }

        private void spellChechButton_Click(object sender, RoutedEventArgs e)
        {
            if (spellChechButton.IsChecked == true)
                editor.IsSpellCheckEnabled = true;
            else
                editor.IsSpellCheckEnabled = false;
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            AboutTeachingTip.IsOpen = true;
        }
    }
}
