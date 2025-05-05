using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Collections.Specialized;
using System.IO;
using Newtonsoft.Json;
using System.Web;
using System.Drawing.Imaging;
using Gecko;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Globalization;

namespace LingQ_Picture_Flashcards
{
    public partial class Form1 : Form
    {

        private const string OpenAIApiKey = "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";
        private const string Gpt3Model = "gpt-3.5-turbo-0125";
        private const string Gpt4Model = "gpt-4-0125-preview";
        private const string PromptDivideIntoWords = "divide the text into words that will be translated for flashcards to teach your students. notice phrasal translation for example \"take away\" is a phrase, do not seperate it into \"take\" and \"away\" . List all words or phrases , each per line. The text is: ";
        private const string PromptTranslate = ". translate each line, write only translation. The text is: ";
        private const string PromptTranslateSentencesAllLangauges = "translate each line, write only translation. Translate into spanish,english,hebrew,russian,korean. output should be in this format es:spanish sentence|en:english sentence |iw:hebrew sentece and so on. The text is: ";
        private const string PromptTranslateWordsAllLangauges = "translate each line, write only translation. format of each line is words|part of speech, translate only the words/phrases without part of speech, look on part of speech when deciding on translation. Translate into spanish,english,hebrew,russian,korean. output should be in this format es:spanish words|en:english words |iw:hebrew words and so on. for example: right|Adj output in hebrew  will be iw:ימינה    The text is: ";

        private string PromptWords_ToInfo = "take each word from the list and write information about it in this format - word|part of speech|root word|level by CEFR. " +
            "example:" + Environment.NewLine + 
            "right|Noun,Adj|right|1" + Environment.NewLine +
            "fell down|Verb|fell|2" + Environment.NewLine +
            "check the level of word if its A1 write 1 if A2 write 2 if B1 write 3 if B2 write 4, if C1 write 5, if C2 write 6 don't add explanations, just write in the format i wrote. part of speech cannot be \"Phrase\", if a word is a phrase, decide what part of speech is this phrase based on the words inside it (verb,noun, adj and so on). this is the list of words:";

        private string PromptVerbs_ToSentences = "each line is a verb.for each verb create 2 sentences with using these combinations of part of speech. each sentence will contain the word in its exact form. create sentence in the langauge of the word. sentences will be logical and normal. for example the word \"give up\", the sentence must contain give up." +
            "Verb + adverb (sentence must contain one verb and one adverb only). " +
            "pronoun + Verb + noun. pronoun + Verb + adjective. write only the senteces seperated by | for each word. use only words from english A1 . Words you write in sentence must be from level A1. write only 2 sentences seperated by |" +
            " example:the verbs " + Environment.NewLine +
            "give up" + Environment.NewLine +
            "running" + Environment.NewLine +
            "ran" + Environment.NewLine + Environment.NewLine +
            "output will be:" + Environment.NewLine +
            "give up easily|give up candies" + Environment.NewLine +
            "running on the road|running slowly" + Environment.NewLine +
            "ran on mud|ran fast" + Environment.NewLine +
            "List of verbs to do:" + Environment.NewLine;



        private string PromptSplitSentence = "Each line in the following text is in this format original_sentence=translated_sentence." +
            "divide the original_sentence into words, and match each word in original to word in translated." +
            "learn from this example:" + Environment.NewLine;

        private string PromptSplitSentenceGeneral = "Each line in the following text is in this format original_sentence=translated_sentence." +
            "divide the original_sentence into words, and match each word in original to word in translated." +
            "In this example original_sentence is english and translated_sentence is russian. learn from this example:" + Environment.NewLine;

        private string PromptSplitSentenceExamples_English = "I love to fart=Мне нравится пукать" + Environment.NewLine +
            "Green ice cream=Зеленое мороженое" + Environment.NewLine +
            "Giving up is hard=Сдаваться сложно" + Environment.NewLine +
            "I fell down=Я упал" + Environment.NewLine +
            "He goes to the store=Он идет в магазин" + Environment.NewLine +
            "He finds each item=Он находит каждый товар" + Environment.NewLine +
            "He needs to buy=Ему нужно купить" + Environment.NewLine +
            Environment.NewLine +
            "the output should be :" + Environment.NewLine +
            "I=Мне|love=нравится|to fart=пукать" + Environment.NewLine +
            "Green=Зеленое|ice cream=мороженое" + Environment.NewLine +
            "Giving up=Сдаваться|is hard=сложно" + Environment.NewLine +
            "I=Я|fell down=упал" + Environment.NewLine +
            "He=Он|goes=идет|to=в|the store=магазин" + Environment.NewLine +
            "He=Он|finds=находит|each=каждый|item=товар" + Environment.NewLine +
            "He=Ему|needs=нужно|to buy=купить" + Environment.NewLine +
            "Write only the output, text can be in different langauges but learn from the example about the structure, The text is: ";

        private string PromptSplitSentenceExamples_Russian = "Мне нравится пукать=I love to fart" + Environment.NewLine +
            "Зеленое мороженое=green ice cream" + Environment.NewLine +
            "Сдаваться сложно=giving up is hard" + Environment.NewLine +
            "Я упал=i fell down" + Environment.NewLine +
            "Он идет в магазин=He goes to the store" + Environment.NewLine +
            "Он находит каждый товар=He finds each item" + Environment.NewLine +
            "Ему нужно купить=He needs to buy" + Environment.NewLine +
            Environment.NewLine +
            "the output should be :" + Environment.NewLine +
            "Мне=I|нравится=love|пукать=to fart" + Environment.NewLine +
            "Зеленое=green|мороженое=ice cream" + Environment.NewLine +
            "Сдаваться=giving up|сложно=is hard" + Environment.NewLine +
            "Я=i|упал=fell down" + Environment.NewLine +
            "Он=He|идет=goes|в=to|магазин=the store" + Environment.NewLine +
            "Он=He|находит=finds|каждый=each|товар=item" + Environment.NewLine +
            "Ему=He|нужно=needs|купить=to buy" + Environment.NewLine +
            "Write only the output, The text is: ";

        public Form1()
        {
        
            InitializeComponent();
            if (File.Exists("CoursesLatest.txt"))
            {
                CurrentCourse = File.ReadAllText("CoursesLatest.txt");
                //listBoxCourses.SelectedItem = CurrentCourse;
            }


            //combo = new MultiColumnComboBox();
            //combo.Name = "combo";
            //tabPage1.Controls.Add(combo);
            //combo.Parent = tabPage1;
            //combo.Height = comboBoxM.Height;
            //combo.Width = comboBoxM.Width;
            //combo.Left = comboBoxM.Left;
            //combo.Top = comboBoxM.Top;
            //combo.TextChanged += comboBoxM_SelectedIndexChanged;

            if (File.Exists("CoursesAll.txt"))
            {
                List<CoursesDesc> jarray = JsonConvert.DeserializeObject<List<CoursesDesc>>(File.ReadAllText("CoursesAll.txt"));
                //CourseListUpdate(jarray);
            }

            Xpcom.Initialize("Firefox");
            geckoWebBrowser = new GeckoWebBrowser { Dock = DockStyle.Fill };
            tabPage9.Controls.Add(geckoWebBrowser);

            geckoWebBrowserPictures = new GeckoWebBrowser { Dock = DockStyle.Fill };
            tabPage4.Controls.Add(geckoWebBrowserPictures);


           // comboBoxM.Visible = false;
            //geckoWebBrowserSounds = new GeckoWebBrowser { Dock = DockStyle.Fill };
            //tabPage5.Controls.Add(geckoWebBrowserSounds);
        }
        GeckoWebBrowser geckoWebBrowser;
        GeckoWebBrowser geckoWebBrowserPictures;
        // GeckoWebBrowser geckoWebBrowserSounds;



        private DirectSoundOut MediaPlayerM;
        private DirectSoundOut MediaPlayerSound;
        private AudioFileReader audioFileM;
        private AudioFileReader audioFileSound;
        private float SoundVolume = 0.1f;
        private Guid DeviceSound;
        private Guid DeviceM;
        private string DeviceSoundStr = "Speakers (Real";//;
        private string DeviceMStr = "USB PnP Sound Device)";//"Speakers (USB PnP Sound Device)";

        private void SetCourseLibrary()
        {
            if (Directory.Exists(Global.CoursesLibrary))
            {
                Directory.CreateDirectory(Global.CoursesLibrary);
            }

            if (CurrentCourseCode != "")
            {
                Global.CurrentCourseLibrary = Global.CoursesLibrary + "\\" + CurrentCourseCode;
                if (Directory.Exists(Global.CurrentCourseLibrary))
                {
                    Directory.CreateDirectory(Global.CurrentCourseLibrary);
                }
            }
            this.Text = "LingQ Picture Flashcards - " + CurrentCourse;


            //Global.DaysLibraryPath = Global.CurrentCourseLibrary + "\\" + Global.DaysLibrary;

            //if (!Directory.Exists(Global.DaysLibraryPath))
            //{
            //    Directory.CreateDirectory(Global.DaysLibraryPath);
            //}

            //Global.DecksLibraryPath = Global.CurrentCourseLibrary + "\\" + Global.DecksLibrary;

            //if (!Directory.Exists(Global.DecksLibraryPath))
            //{
            //    Directory.CreateDirectory(Global.DecksLibraryPath);
            //}

            Global.PicturesLibraryPath = Global.CurrentCourseLibrary + "\\" + Global.PicturesLibrary;

            if (!Directory.Exists(Global.PicturesLibraryPath))
            {
                Directory.CreateDirectory(Global.PicturesLibraryPath);
            }

            Global.SoundsLibraryPath = Global.CurrentCourseLibrary + "\\" + Global.SoundsLibrary;

            if (!Directory.Exists(Global.SoundsLibraryPath))
            {
                Directory.CreateDirectory(Global.SoundsLibraryPath);
            }


            Global.PresentLibraryPath = Global.CurrentCourseLibrary + "\\" + Global.PresentLibrary;
            Global.PresentLibraryPathOriginal = Global.PresentLibraryPath;

            if (!Directory.Exists(Global.PresentLibraryPath))
            {
                Directory.CreateDirectory(Global.PresentLibraryPath);
            }

            Global.ReadyLibraryPath = Global.CurrentCourseLibrary + "\\" + Global.ReadyLibrary;

            if (!Directory.Exists(Global.ReadyLibraryPath))
            {
                Directory.CreateDirectory(Global.ReadyLibraryPath);
            }


            Global.DoneLibraryPath = Global.CurrentCourseLibrary + "\\" + Global.DoneLibrary;

            if (!Directory.Exists(Global.DoneLibraryPath))
            {
                Directory.CreateDirectory(Global.DoneLibraryPath);
            }

            Global.NeverLibraryPath = Global.CurrentCourseLibrary + "\\" + Global.NeverLibrary;

            if (!Directory.Exists(Global.NeverLibraryPath))
            {
                Directory.CreateDirectory(Global.NeverLibraryPath);
            }
            //Global.LingQFilePath = Global.CurrentCourseLibrary + "\\" + Global.LingQFile;

            //LoadPresentFlashcards();
            if (!setCombo)
            {
                setCombo = true;
                DataTable dt = new DataTable();
                dt.Clear();
                dt.Columns.Add("Name");
                dt.Columns.Add("Latest");
                dt.Columns.Add("AVG1");
                dt.Columns.Add("AVG1_counted");
                dt.Columns.Add("NoDeck");
                dt.Columns.Add("Deck1");
                dt.Columns.Add("Deck2");
                dt.Columns.Add("Total");


                combo.ColumnsToDisplay = new
    string[] { "Name", "Latest", "AVG1", "AVG1_counted", "NoDeck", "Deck1", "Deck2", "Total"};//columns to display
                combo.DisplayMember = "Name";

                combo.Table = dt;
//                combo.Items.Add(" ");
                string[] Mfiles = Directory.GetFiles(Global.CurrentCourseLibrary + "\\M");
                Statistics_Of_Lib stats_root = GetLibraryStatistics(Global.PresentLibraryPathOriginal);
                if (stats_root != null)
                {
                    double average = stats_root.TimesDeck1 / stats_root.TotalCountedDeck1;
                    DataRow _ravi = dt.NewRow();
                    _ravi["Name"] = " ";
                    _ravi["Latest"] = stats_root.LatestDate.ToString("dd/MM");
                    _ravi["AVG1"] = Math.Round(average, 2).ToString();
                    _ravi["AVG1_counted"] = stats_root.TotalCountedDeck1;
                    _ravi["NoDeck"] = stats_root.TotalInNoDeck;
                    _ravi["Deck1"] = stats_root.TotalInDeck1;
                    _ravi["Deck2"] = stats_root.TotalInDeck2;
                    _ravi["Total"] = stats_root.TotalCards;

                    dt.Rows.Add(_ravi);
                }

                foreach (string file in Mfiles)
                {
                    Statistics_Of_Lib stats = GetLibraryStatistics(Global.PresentLibraryPathOriginal + "\\" + Path.GetFileNameWithoutExtension(file));
                    if (stats != null)
                    {
                        double average = stats.TimesDeck1 / stats.TotalCountedDeck1;
                        DataRow _ravi = dt.NewRow();
                        _ravi["Name"] = Path.GetFileNameWithoutExtension(file);
                        _ravi["Latest"] = stats.LatestDate.ToString("dd/MM");
                        _ravi["AVG1"] = Math.Round(average, 2).ToString();
                        _ravi["AVG1_counted"] = stats.TotalCountedDeck1;
                        _ravi["NoDeck"] = stats.TotalInNoDeck;
                        _ravi["Deck1"] = stats.TotalInDeck1;
                        _ravi["Deck2"] = stats.TotalInDeck2;
                        _ravi["Total"] = stats.TotalCards;

                        dt.Rows.Add(_ravi);
                    }
                    else
                    {
                        DataRow _ravi = dt.NewRow();
                        _ravi["Name"] = Path.GetFileNameWithoutExtension(file);
                        _ravi["Latest"] = "";
                        _ravi["AVG1"] = "";
                        _ravi["AVG1_counted"] = "";
                        _ravi["NoDeck"] = "";
                        _ravi["Deck1"] = "";
                        _ravi["Deck2"] = "";
                        _ravi["Total"] = "";
                        dt.Rows.Add(_ravi);
                    }
                }
               // combo.SelectedItem = " ";
            }
        }
        bool setCombo = false;

        string CurrentCourse = "";
        string CurrentCourseCode = "";
        Dictionary<string, CoursesDesc> CoursesList = new Dictionary<string, CoursesDesc>();
        Dictionary<string, LingQDesc> PresentList = new Dictionary<string, LingQDesc>();
        List<string> CurrentDeck_Ids = new List<string>();
        List<string> CurrentDeck_Type = new List<string>();
        int CurrentDeck_CardIndex = 0;
        //Dictionary<string, LingQDesc> DoneList = new Dictionary<string, LingQDesc>();



        //private void CourseListUpdate(List<CoursesDesc> jarray)
        //{
        //    CoursesList.Clear();
        //    listBoxCourses.Items.Clear();
        //    foreach (CoursesDesc course in jarray)
        //    {
        //        CoursesList.Add(course.title, course);
        //        listBoxCourses.Items.Add(course.title);
        //    }

        //    if (CurrentCourse != "")
        //    {
        //        CurrentCourseCode = CoursesList[CurrentCourse].code;
        //        listBoxCourses.SelectedItem = CurrentCourse;
        //    }
        //    else
        //    {
        //        if (listBoxCourses.Items.Count > 0)
        //        {
        //            CurrentCourse = listBoxCourses.Items[0].ToString();
        //            listBoxCourses.SelectedItem = CurrentCourse;
        //            File.WriteAllText("CoursesLatest.txt", CurrentCourse);
        //            CurrentCourseCode = CoursesList[CurrentCourse].code;
        //        }
        //    }

        //    SetCourseLibrary();
        //}


        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (var dev in DirectSoundOut.Devices)
            {
                if (dev.Description == DeviceSoundStr)
                {
                    DeviceSound = dev.Guid;
                }
                else if (dev.Description.IndexOf( DeviceMStr) >= 0)
                {
                    DeviceM = dev.Guid;
                }
            }
            //comboBoxDeckNumber.SelectedItem = "1";
            CenterCardStuff();
            HideAllCardElements();
            ShowButtonsAroundCard(false);
            LoadCardsForToday(-1);
            labelSound.Text = "";
            CreateNewCard_Click(null, null);



        }

        MultiColumnComboBox combo;

        private void listBoxCourses_SelectedIndexChanged(object sender, EventArgs e)
        {
            //CurrentCourse = listBoxCourses.SelectedItem.ToString();
            //if (CurrentCourse != "")
            //{
            //    File.WriteAllText("CoursesLatest.txt", CurrentCourse);

            //    CurrentCourseCode = CoursesList[CurrentCourse].code;
            //    SetCourseLibrary();
            //}
        }

        private void Courses_Click(object sender, EventArgs e)
        {
        
        }
        Random random = new Random();

        //string CurrentCreateCardPath = "";
        LingQDesc CurrentCreateCard;
        string CurrentCreateCardSound = "";

        List<LingQDesc> FutureFiles = new List<LingQDesc>();



        private void CreateNewCard_Click(object sender, EventArgs e)
        {
            textBoxSourceLang.Text = "";
            textBoxTargetLang.Text = "";
            textBoxFragment.Text = "";
            labelSound.Text = "";            
            pictureBoxFlashcard.ImageLocation = "";
            CurrentCreateCard = new LingQDesc();
            CurrentCreateCard.hints = new List<Hint>();
            CurrentCreateCard.hints.Add(new Hint());
            CurrentCreateCard.hints[0].locale = "en";

        }



        private void LoadCard_ToCreation(LingQDesc Card)
        {
            //path = "d:\\Projects\\LingQ Picture Flashcards\\LingQ Picture Flashcards\\bin\\Debug\\Courses\\ru\\CardsPresent\\106719367";
            CurrentCreateCard = Card;
            if (CurrentCreateCard != null)
            {
                butAddCardToDecks.BackColor = Color.DarkRed;
                textBoxSourceLang.Text = CurrentCreateCard.term;
                textBoxTargetLang.Text = CurrentCreateCard.hints[0].text;
                textBoxFragment.Text = CurrentCreateCard.fragment;
                checkBoxForgLocal.Checked = CurrentCreateCard.StudyForgToLocal;
                checkBoxLocalForg.Checked = CurrentCreateCard.StudyLocalToForg;
                checkBoxSoundLocal.Checked = CurrentCreateCard.StudySoundToLocal;
                checkBoxSoundSpelling.Checked = CurrentCreateCard.StudySoundToSpell;
                if(File.Exists(Global.SoundsLibraryPath + "\\" + CurrentCreateCard.id + ".mp3"))
                {
                    CurrentCreateCardSound = Global.SoundsLibraryPath + "\\" + CurrentCreateCard.id + ".mp3";
                    labelSound.Text = Global.SoundsLibraryPath + "\\" + CurrentCreateCard.id + ".mp3";
                }

                if (CurrentCreateCard.path == "")
                {
                    CurrentCreateCard.path = Global.PresentLibraryPath + "\\" + CurrentCreateCard.id;
                }

                if (File.Exists(Global.PicturesLibraryPath + "\\" + CurrentCreateCard.id + ".jpg"))
                {
                    //pictureBoxFlashcard.ImageLocation = Global.PicturesLibraryPath + "\\" + CurrentCreateCard.id + ".jpg";

                        pictureBoxFlashcard.Image = LoadBitmapUnlocked(Global.PicturesLibraryPath + "\\" + CurrentCreateCard.id + ".jpg");
                }
                butBrowse_Click(null,null);
            }
        }




        private void butBrowse_Click(object sender, EventArgs e)
        {
            if (textBoxSourceLang.Text != "")
            {
                try
                {
                    geckoWebBrowserSounds.Navigate("https://forvo.com/word/" + textBoxSourceLang.Text + "/#" + CurrentCourseCode);
                    string tohtml = HttpUtility.UrlEncode(textBoxSourceLang.Text);
                    geckoWebBrowserPictures.Navigate("https://www.google.com/search?q=" + tohtml + "&newwindow=1&source=lnms&tbm=isch");
                    //webBrowserTranslate.Navigate("https://translate.google.com/#" + CurrentCourseCode +"/" + CurrentCreateCard.hints[0].locale +"/" + tohtml);
                    geckoWebBrowser.Navigate("https://translate.google.com/#view=home&op=translate&sl=" + CurrentCourseCode + "&tl=" + CurrentCreateCard.hints[0].locale + "&text=" + tohtml);
                    //geckoWebBrowser.Navigate("https://" + CurrentCreateCard.hints[0].locale + CurrentCourseCode + ".dict.cc/?s=" + tohtml);

                    tabControl1.TabPages[1].Focus();
                }
                catch { }
            }
            else
            {
                MessageBox.Show("Get New Card First");
            }
        }


        private void butBrowseTranslated_Click(object sender, EventArgs e)
        {
            if (textBoxSourceLang.Text != "")
            {
                string tohtml = HttpUtility.UrlEncode(textBoxTargetLang.Text);
                geckoWebBrowserPictures.Navigate("https://www.google.com/search?q=" + tohtml + "&newwindow=1&source=lnms&tbm=isch");
            }
            else
            {
                MessageBox.Show("Get New Card First");
            }
        }


        private void butCreateCardPasteFromClipboardImage_Click(object sender, EventArgs e)
        {
            if (Clipboard.GetDataObject().GetDataPresent("Bitmap"))
            {
                object o = Clipboard.GetDataObject().GetData("Bitmap");
                if (o != null)
                {
                    pictureBoxFlashcard.SizeMode = PictureBoxSizeMode.StretchImage;
                    pictureBoxFlashcard.Image = (Image)o;
                    if (labelSound.Text != "")
                    {
                        butAddCardToDecks.BackColor = Color.DarkGreen;
                    }
                }
            }

        }

        private void butGetLatestSound_Click(object sender, EventArgs e)
        {
            
            var directory = new DirectoryInfo(textBoxSounds.Text);
            FileInfo[] files = directory.GetFiles();
                DateTime LatestTime = new DateTime();
            FileInfo myFile = null;
            foreach (FileInfo file in files)
            {
                if(DateTime.Compare(file.LastWriteTime, LatestTime) > 0)
                {
                    myFile = file;
                    LatestTime = file.LastWriteTime;
                }
            }


            if(myFile != null)
            {
                CurrentCreateCardSound = myFile.FullName;
                labelSound.Text = CurrentCreateCardSound;

                MediaPlayerSound = new DirectSoundOut(DeviceSound);
                audioFileSound = new AudioFileReader(CurrentCreateCardSound);

                audioFileSound.Volume = SoundVolume;
                MediaPlayerSound.Init(audioFileSound);
                MediaPlayerSound.Play();


                //wplayer.URL = "";
                if (pictureBoxFlashcard.Image != null)
                {
                    butAddCardToDecks.BackColor = Color.DarkGreen;
                }
            }
        }
        



        private void butAddCardToDecks_Click(object sender, EventArgs e)
        {

            CurrentCreateCard.StudyForgToLocal = checkBoxForgLocal.Checked;
            CurrentCreateCard.StudyLocalToForg = checkBoxLocalForg.Checked;
            CurrentCreateCard.term = textBoxSourceLang.Text;
            CurrentCreateCard.fragment = textBoxFragment.Text;
            CurrentCreateCard.hints[0].text = textBoxTargetLang.Text;

            if (CurrentCreateCard.path == "")
            {
                string temp = "";
                CurrentCreateCard.id = random.Next(0, 1000000);
                CurrentCreateCard.path = Global.ReadyLibraryPath + "\\" + CurrentCreateCard.id;
                temp = Global.PresentLibraryPath + "\\" + CurrentCreateCard.id;
                while (File.Exists(CurrentCreateCard.path) || File.Exists(temp))
                {
                    CurrentCreateCard.id = random.Next(0, 1000000);
                    CurrentCreateCard.path = Global.ReadyLibraryPath + "\\" + CurrentCreateCard.id;
                    temp = Global.PresentLibraryPath + "\\" + CurrentCreateCard.id;
                }
            }

            //CurrentCreateCard.path = 
            if (CurrentCreateCardSound != "")
            {
                try
                {

                    if (CurrentCreateCardSound.IndexOf(Global.SoundsLibraryPath) < 0)
                    {
                        File.Copy(CurrentCreateCardSound, Global.SoundsLibraryPath + "\\" + CurrentCreateCard.id + ".mp3");
                    }
                }
                catch
                {
                    MessageBox.Show("didn't save sound");
                }
                CurrentCreateCard.StudySoundToLocal = checkBoxSoundLocal.Checked;
                CurrentCreateCard.StudySoundToSpell = checkBoxSoundSpelling.Checked;
            }
            else
            {
                CurrentCreateCard.StudySoundToLocal = false;
                CurrentCreateCard.StudySoundToSpell = false;
            }
            if (pictureBoxFlashcard.Image != null)
            {
                try
                {

                    pictureBoxFlashcard.Image.Save(Global.PicturesLibraryPath + "\\" + CurrentCreateCard.id + ".jpg");


                }
                catch
                {
                    MessageBox.Show("didn't save image");
                }
            }

            if (CurrentCreateCard.path.IndexOf(Global.PresentLibraryPath) < 0)
            {

                File.WriteAllText(CurrentCreateCard.path, JsonConvert.SerializeObject(CurrentCreateCard));
                
            }
            else
            {
                LoadCurrentCardInDeck(false);
                SaveCurrentCard();
            }
                labelSound.Text = "";
                pictureBoxFlashcard.Image = null;
                pictureBoxFlashcard.ImageLocation = null;
                CurrentCreateCardSound = "";
                CreateNewCard_Click(null, null);

        }

        private void butTestSound_Click(object sender, EventArgs e)
        {
            if (CurrentCreateCardSound != "")
            {
                MediaPlayerSound = new DirectSoundOut(DeviceSound);
                audioFileSound = new AudioFileReader(CurrentCreateCardSound);
                audioFileSound.Volume = SoundVolume;
                MediaPlayerSound.Init(audioFileSound);
                MediaPlayerSound.Play();

                //wplayer.URL = "";
            }
        }

        private void ShowButtonsAroundCard(bool show)
        {
            butDeck1.Visible = show;
            butDeck2.Visible = show;
            //butDeck3.Visible = show;
            //butDeck4.Visible = show;
            //butDeck5.Visible = show;
            //butDeck5_60.Visible = show;
            //butDeck5_180.Visible = show;
            //butStudyLater.Visible = show;
            labelCardNum.Visible = show;
            //butEditCurrentCard.Visible = show;
        }

        private void HideAllCardElements()
        {
            //groupBoxNavigate.Visible = false;
            labelCardFront.Visible = false;
            //labelCardFrontFragment.Visible = false;
            butCardPlayAgain.Visible = false;
            butCardPrevious.Visible = false;
            butCardNext.Visible = false;
            labelSpelling.Visible = false;
            textBoxSpelling.Visible = false;
            labelWriteAndPressEnter.Visible = false;
            butCardShowPicture.Visible = false;
            pictureBoxCardPicture.Visible = false;
            //butCardFlip.Visible = false;
            labelCardBack.Visible = false;
            //labelCardBackFragment.Visible = false;
        }

        private void CenterCardStuff()
        {
            labelCardNum.Left = tabPage1.Width / 2 - labelCardNum.Width/2;
            //labelCardFrontFragment.Left = tabPage1.Width / 2 - labelCardFrontFragment.Width / 2;
            labelCardFront.Left = tabPage1.Width / 2 - labelCardFront.Width / 2;
            //groupBoxNavigate.Left = tabPage1.Width / 2 - groupBoxNavigate.Width / 2;

            //butCardPrevious.Top = butCardPlayAgain.Top;
            //butCardNext.Top = butCardPlayAgain.Top;
            //butCardPrevious.Left = butCardPlayAgain.Left - 60 - butCardPrevious.Width;
            //butCardNext.Left = butCardPlayAgain.Left + 60 + butCardPlayAgain.Width;

            textBoxSpelling.Left = tabPage1.Width / 2 - textBoxSpelling.Width / 2;
            labelWriteAndPressEnter.Left = textBoxSpelling.Right + 10;
            labelWriteAndPressEnter.Top = textBoxSpelling.Top;

            labelSpelling.Left = textBoxSpelling.Left - 10 - labelSpelling.Width;
            labelSpelling.Top = textBoxSpelling.Top;

            butCardShowPicture.Left = tabPage1.Width / 2 - butCardShowPicture.Width / 2;
            pictureBoxCardPicture.Left = tabPage1.Width / 2 - pictureBoxCardPicture.Width / 2;
            //butCardFlip.Left = tabPage1.Width / 2 - butCardFlip.Width / 2;
            labelCardBack.Left = tabPage1.Width / 2 - labelCardBack.Width / 2;
            //labelCardBackFragment.Left = tabPage1.Width / 2 - labelCardBackFragment.Width / 2;

            pictureBoxCardPicture.Height = this.Height - butCardShowPicture.Top - 10 ;
            pictureBoxCardPicture.Width = pictureBoxCardPicture.Height;
            pictureBoxCardPicture.Top = butCardShowPicture.Top;
            pictureBoxCardPicture.Left = tabPage1.Width / 2 - pictureBoxCardPicture.Width / 2;

            //pictureBoxBackground.Height = pictureBoxCardPicture.Height;
            //pictureBoxBackground.Top = pictureBoxCardPicture.Top;
            //pictureBoxBackground.Width = this.Width;
            //pictureBoxBackground.Left = 0;

            labelBigRedWord.Left = this.Width / 2 - labelBigRedWord.Width / 2;
            //labelBigRedWord.Top = pictureBoxBackground.Top;

            labelBigRedWordTranslation.Left = this.Width / 2 - labelBigRedWordTranslation.Width / 2;
            //labelBigRedWordTranslation.Top = pictureBoxBackground.Top + labelBigRedWord.Height + 5 ;

        }

        private void butGetReadyCard_Click(object sender, EventArgs e)
        {

        }
        string LoadedCardSoundPath = "";

        private void PlaySound_OrM(bool ShowAllCard,int lingqid, bool KeepButtons)
        {
            if ((combo.Text != null && combo.Text.ToString() == " ") )//|| checkBoxForceSound.Checked)
            {
                if (File.Exists(Global.SoundsLibraryPath + "\\" + lingqid + ".mp3") || lingqid < 0)
                {
                    if (lingqid >= 0)
                    {
                        LoadedCardSoundPath = Global.SoundsLibraryPath + "\\" + lingqid + ".mp3";

                        MediaPlayerSound = new DirectSoundOut(DeviceSound);
                        audioFileSound = new AudioFileReader(LoadedCardSoundPath);
                        audioFileSound.Volume = SoundVolume;
                        MediaPlayerSound.Init(audioFileSound);
                    }
                    if (LoadedCardSoundPath != "")
                    {
                        if (ShowAllCard)
                        {
                            MediaPlayerSound.Play();
                            if (!KeepButtons)
                            {
                                butCardPlayAgain.Visible = false;
                                //checkBoxForceSound.Visible = false;
                                //butPlayM.Visible = false;
                            }
                        }
                        else
                        {
                            MediaPlayerSound.Stop();
                            butCardPlayAgain.Visible = true;
                            //checkBoxForceSound.Visible = false;
                            //butPlayM.Visible = false;
                        }
                    }
                }
                else
                {
                    butCardPlayAgain.Visible = false;
                    //checkBoxForceSound.Visible = false;
                    //butPlayM.Visible = false;
                }
            }
            else if (CurrentMFile != "" && ShowAllCard)
            {
                if (LoadedCardSoundPath != "")
                {
                    MediaPlayerSound = new DirectSoundOut(DeviceSound);
                    audioFileSound = new AudioFileReader(LoadedCardSoundPath);
                    audioFileSound.Volume = SoundVolume;
                    MediaPlayerSound.Init(audioFileSound);
                    MediaPlayerSound.Play();
                }
                MediaPlayerM = new DirectSoundOut(DeviceM);
                audioFileM = new AudioFileReader(CurrentMFile);
                MediaPlayerM.Init(audioFileM);
                MediaPlayerM.Play();

                if (ShowAllCard)
                {
                    if (!KeepButtons)
                    {
                        butCardPlayAgain.Visible = false;
                        //checkBoxForceSound.Visible = false;
                        //butPlayM.Visible = false;
                    }
                }
                else
                {
                    butCardPlayAgain.Visible = true;
                    //checkBoxForceSound.Visible = true;
                    //butPlayM.Visible = true;
                }
            }

        }


        private void LoadCurrentCardInDeck(bool ShowAllCard)
        {
            labelBigRedWord.Visible = false;
            labelBigRedWordTranslation.Visible = false;
            ShowButtonsAroundCard(true);
            LoadedCardSoundPath = "";
            HideAllCardElements();

            //groupBoxNavigate.Visible = true;
            butCardPlayAgain.Visible = true;
            
            if (CurrentDeck_CardIndex < CurrentDeck_Ids.Count && CurrentDeck_CardIndex >= 0)
            {
                if (CurrentDeck_Ids.Count > 1)
                {
                    butCardPrevious.Visible = true;
                    butCardNext.Visible = true;
                }
                
                labelCardNum.Visible = true;
                LingQDesc lingq = PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]];

                if (File.Exists(Global.SoundsLibraryPath + "\\" + lingq.id + ".mp3"))
                {
                    LoadedCardSoundPath = Global.SoundsLibraryPath + "\\" + lingq.id + ".mp3";
                }


                        if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateForgToLocal")
                {
                    labelCardNum.Text = "Card:" + (CurrentDeck_CardIndex + 1) + "/" + CurrentDeck_Ids.Count + " Days : " + PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayForgToLocal.ToString();
                    labelCardFront.Text = lingq.term;
                    labelCardFront.Visible = true;
                    //labelCardFrontFragment.Text = lingq.fragment;
                    //labelCardFrontFragment.Visible = true;

                    PlaySound_OrM(ShowAllCard, lingq.id,false);

                    if (File.Exists(Global.PicturesLibraryPath + "\\" + lingq.id + ".jpg"))
                    {
                        pictureBoxCardPicture.Image = LoadBitmapUnlocked(Global.PicturesLibraryPath + "\\" + lingq.id + ".jpg");


                        //pictureBoxCardPicture.ImageLocation =;
                        //pictureBoxCardPicture.Load();
                        if (ShowAllCard)
                        {
                            pictureBoxCardPicture.Visible = true;
                        }
                        else
                        {
                            butCardShowPicture.Visible = true;
                        }
                        
                    }
                    else
                    {
                        pictureBoxCardPicture.Image = null;
                        pictureBoxCardPicture.ImageLocation = null;
                    }
                    //butCardFlip.Visible = true;
                    labelCardBack.Text = lingq.hints[0].text;
                    if (ShowAllCard)
                    {
                        labelCardBack.Visible = true;
                        //labelCardBackFragment.Visible = true;
                        if ((IsSlowPlay && checkBoxBigRedWord.Checked) || (!IsSlowPlay && checkBoxShowBigRedWordFast.Checked))
                        {
                            labelBigRedWord.Visible = true;
                            labelBigRedWordTranslation.Visible = true;
                        }
                    }
                    //labelCardBackFragment.Text = "";
                    labelBigRedWord.Text = lingq.term;
                    labelBigRedWordTranslation.Text = lingq.hints[0].text;
                    TimeSpan gap = DateTime.Now - lingq.LastShowLocalToForg;
                    //if (gap.TotalDays >= 30 && lingq.LastShowLocalToForg != DateTime.MinValue)
                    //{
                    //    //numericUpDownDeck5Increase.Value = Convert.ToInt32(Math.Round(gap.TotalDays) * 2);
                    //}

                    lingq.LastShowLocalToForg = DateTime.Now;

                    //if (LoadedCardSoundPath != "")
                    //{
                    //    WMPLib.WindowsMediaPlayer wplayer = new WMPLib.WindowsMediaPlayer();

                    //    wplayer.URL = LoadedCardSoundPath;
                    //    wplayer.controls.play();
                    //}
                }
                else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateLocalToForg")
                {
                    labelCardNum.Text = "Card:" + (CurrentDeck_CardIndex + 1) + "/" + CurrentDeck_Ids.Count + " Days : " + PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayLocalToForg.ToString();
                    labelCardFront.Text = lingq.hints[0].text;
                    labelCardFront.Visible = true;
                    labelBigRedWord.Text = lingq.term;
                    labelBigRedWordTranslation.Text = lingq.hints[0].text;
                    PlaySound_OrM(ShowAllCard, lingq.id,false);

                    if (File.Exists(Global.PicturesLibraryPath + "\\" + lingq.id + ".jpg"))
                    {

                            pictureBoxCardPicture.Image = LoadBitmapUnlocked(Global.PicturesLibraryPath + "\\" + lingq.id + ".jpg");
                        
                        if (ShowAllCard)
                        {
                            pictureBoxCardPicture.Visible = true;
                        }
                        else
                        {
                            butCardShowPicture.Visible = true;
                        }
                    }
                    else
                    {
                        pictureBoxCardPicture.Image = null;
                        pictureBoxCardPicture.ImageLocation = null;
                    }
                    //butCardFlip.Visible = true;
                    labelCardBack.Text = lingq.term;
                    //labelCardBackFragment.Text = lingq.fragment;
                    if (ShowAllCard)
                    {
                        labelCardBack.Visible = true;
                        //labelCardBackFragment.Visible = true;
                        if ((IsSlowPlay && checkBoxBigRedWord.Checked) || (!IsSlowPlay && checkBoxShowBigRedWordFast.Checked))
                        {
                            labelBigRedWord.Visible = true;
                            labelBigRedWordTranslation.Visible = true;
                        }
                    }
                    //TimeSpan gap = DateTime.Now - lingq.LastShowForgToLocal;
                    //if (gap.TotalDays >= 30 && lingq.LastShowForgToLocal != DateTime.MinValue)
                    //{
                    //    numericUpDownDeck5Increase.Value = Convert.ToInt32(Math.Round(gap.TotalDays) * 2);
                    //}

                    lingq.LastShowForgToLocal = DateTime.Now;
                }
                else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToLocal")
                {
                    labelCardNum.Text = "Card:" + (CurrentDeck_CardIndex + 1) + "/" + CurrentDeck_Ids.Count + " Days : " + PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToLocal.ToString();
                    labelBigRedWord.Text = lingq.term;
                    labelBigRedWordTranslation.Text = lingq.hints[0].text;
                    PlaySound_OrM(ShowAllCard, lingq.id,false);

                    if (File.Exists(Global.PicturesLibraryPath + "\\" + lingq.id + ".jpg"))
                    {
                        pictureBoxCardPicture.Image = LoadBitmapUnlocked(Global.PicturesLibraryPath + "\\" + lingq.id + ".jpg");
                        if (ShowAllCard)
                        {
                            pictureBoxCardPicture.Visible = true;
                        }
                        else
                        {
                            butCardShowPicture.Visible = true;
                        }
                    }
                    else
                    {
                        pictureBoxCardPicture.Image = null;
                        pictureBoxCardPicture.ImageLocation = null;
                    }

                    //labelCardBack.Text = lingq.hints[0].text;
                    //labelCardBackFragment.Text = "";
                    labelCardFront.Text = "";
                    labelCardBack.Text = lingq.term + " - " + lingq.hints[0].text;
                    //labelCardBackFragment.Text = lingq.fragment;

                    if (ShowAllCard)
                    {
                        labelCardBack.Visible = true;
                        //labelCardBackFragment.Visible = true;
                        //butCardFlip.Visible = false;
                        if ((IsSlowPlay && checkBoxBigRedWord.Checked) || (!IsSlowPlay && checkBoxShowBigRedWordFast.Checked))
                        {
                            labelBigRedWord.Visible = true;
                            labelBigRedWordTranslation.Visible = true;
                        }
                    }
                    else
                    {
                        //butCardFlip.Visible = true;
                    }

                    //TimeSpan gap = DateTime.Now - lingq.LastShowSoundToLocal;
                    //if (gap.TotalDays >= 30 && lingq.LastShowSoundToLocal != DateTime.MinValue)
                    //{
                    //    numericUpDownDeck5Increase.Value = Convert.ToInt32(Math.Round(gap.TotalDays) * 2);
                    //}

                    lingq.LastShowSoundToLocal = DateTime.Now;

                    //if (LoadedCardSoundPath != "")
                    //{


                    //    wplayer.URL = LoadedCardSoundPath;
                    //    wplayer.controls.play();
                    //}
                }
                else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToSpell")
                {
                    labelCardNum.Text = "Card:" + (CurrentDeck_CardIndex + 1) + "/" + CurrentDeck_Ids.Count + " Days : " + PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToSpell.ToString();
                    labelBigRedWord.Text = lingq.term;
                    labelBigRedWordTranslation.Text = lingq.hints[0].text;
                    textBoxSpelling.Visible = true;
                    labelSpelling.Visible = true;
                    labelWriteAndPressEnter.Visible = true;

                    if (File.Exists(Global.SoundsLibraryPath + "\\" + lingq.id + ".mp3"))
                    {
                        LoadedCardSoundPath = Global.SoundsLibraryPath + "\\" + lingq.id + ".mp3";

                        MediaPlayerSound = new DirectSoundOut(DeviceSound);
                        audioFileSound = new AudioFileReader(LoadedCardSoundPath);
                        audioFileSound.Volume = SoundVolume;
                        MediaPlayerSound.Init(audioFileSound);
                        MediaPlayerSound.Play();

                        butCardPlayAgain.Visible = true;
                    }
                    if (File.Exists(Global.PicturesLibraryPath + "\\" + lingq.id + ".jpg"))
                    {
                        pictureBoxCardPicture.Image = LoadBitmapUnlocked(Global.PicturesLibraryPath + "\\" + lingq.id + ".jpg");
                        if (ShowAllCard)
                        {
                            pictureBoxCardPicture.Visible = true;
                        }
                        else
                        {
                            butCardShowPicture.Visible = true;
                        }
                    }
                    else
                    {
                        pictureBoxCardPicture.Image = null;
                        pictureBoxCardPicture.ImageLocation = null;
                    }
                    //butCardFlip.Visible = true;
                    labelCardBack.Text = lingq.term + " - " + lingq.hints[0].text;
                    //labelCardBackFragment.Text = lingq.fragment;

                    if (ShowAllCard)
                    {
                        labelCardBack.Visible = true;
                        //labelCardBackFragment.Visible = true;
                        //butCardFlip.Visible = false;
                        if ((IsSlowPlay && checkBoxBigRedWord.Checked) || (!IsSlowPlay && checkBoxShowBigRedWordFast.Checked))
                        {
                            labelBigRedWord.Visible = true;
                            labelBigRedWordTranslation.Visible = true;
                        }
                    }
                    else
                    {
                        //butCardFlip.Visible = true;
                    }

                    textBoxSpelling.Focus();
                    //TimeSpan gap = DateTime.Now - lingq.LastShowSoundToSpell;
                    //if(gap.TotalDays >= 30 && lingq.LastShowSoundToSpell != DateTime.MinValue)
                    //{
                    //    numericUpDownDeck5Increase.Value = Convert.ToInt32( Math.Round(gap.TotalDays) * 2);
                    //}
                    lingq.LastShowSoundToSpell = DateTime.Now;

                    //if (LoadedCardSoundPath != "")
                    //{

                    //    wplayer.URL = LoadedCardSoundPath;
                    //    wplayer.controls.play();
                    //}
                }


            }
            
            
            if (ShowAllCard)
            {
                //butCardFlip.Visible = false;
            }
            Application.DoEvents();
            CenterCardStuff();

        }
        private Bitmap LoadBitmapUnlocked(string file_name)
        {
            try
            {
                using (Bitmap bm = new Bitmap(file_name))
                {
                    return new Bitmap(bm);
                }
            }
            catch
            {
                return null;
            }
        }

        private Statistics_Of_Lib GetLibraryStatistics(string LibPath)
        {
            if (Directory.Exists(LibPath))
            {
                string[] filesstr = Directory.GetFiles(LibPath);
                List<string> files = new List<string>(filesstr);
                Statistics_Of_Lib stats = new Statistics_Of_Lib();
                while (files.Count > 0)
                {
                    string path = files[0];
                    LingQDesc lingQ = JsonConvert.DeserializeObject<LingQDesc>(File.ReadAllText(path));
                    lingQ.CleanAddedSlots();
                    if (lingQ.TimesDeck_ForgToLocal[0] > 0 || lingQ.TimesDeck_LocalToForg[0] > 0)
                    {
                        if (lingQ.TimesDeck_ForgToLocal[1] > 0 && lingQ.TimesDeck_ForgToLocal[0] > 0)
                        {
                            if (DateTime.Compare(lingQ.FirstDateDeck_ForgToLocal[0], lingQ.FirstDateDeck_ForgToLocal[1]) != 0)
                            {
                                stats.TimesDeck1 += lingQ.TimesDeck_ForgToLocal[0];
                                stats.TotalCountedDeck1++;
                            }
                        }
                        if (lingQ.TimesDeck_ForgToLocal[2] > 0 && lingQ.TimesDeck_ForgToLocal[1] > 0)
                        {
                            stats.TimesDeck2 += lingQ.TimesDeck_ForgToLocal[1];
                            stats.TotalCountedDeck2++;
                        }
                        if (lingQ.TimesDeck_ForgToLocal[3] > 0 && lingQ.TimesDeck_ForgToLocal[2] > 0)
                        {
                            stats.TimesDeck3 += lingQ.TimesDeck_ForgToLocal[2];
                            stats.TotalCountedDeck3++;
                        }
                        if (lingQ.TimesDeck_ForgToLocal[4] > 0 && lingQ.TimesDeck_ForgToLocal[3] > 0)
                        {
                            stats.TimesDeck4 += lingQ.TimesDeck_ForgToLocal[3];
                            stats.TotalCountedDeck4++;
                        }
                        stats.TimesDeck5 += lingQ.TimesDeck_ForgToLocal[4];
                        stats.TotalCards++;
                        
                        for (int i = 0; i < 5; i++)
                        {
                            if (DateTime.Compare(lingQ.LatestDateDeck_ForgToLocal[i], stats.LatestDate) > 0)
                            {
                                stats.LatestDate = lingQ.LatestDateDeck_ForgToLocal[i];
                            }
                        }



                        if (lingQ.TimesDeck_ForgToLocal[4] > 0)
                        {
                            stats.TotalInDeck5++;
                        }
                        else if (lingQ.TimesDeck_ForgToLocal[3] > 0)
                        {
                            stats.TotalInDeck4++;
                        }
                        else if (lingQ.TimesDeck_ForgToLocal[2] > 0)
                        {
                            stats.TotalInDeck3++;
                        }
                        else if (lingQ.TimesDeck_ForgToLocal[1] > 0)
                        {
                            stats.TotalInDeck2++;
                        }
                        else if (lingQ.TimesDeck_ForgToLocal[0] > 0)
                        {
                            stats.TotalInDeck1++;
                        }
                        else
                        {
                            stats.TotalInNoDeck++;
                        }


                        if (lingQ.TimesDeck_LocalToForg[1] > 0 && lingQ.TimesDeck_LocalToForg[0] > 0)
                        {
                            if (DateTime.Compare(lingQ.FirstDateDeck_LocalToForg[0], lingQ.FirstDateDeck_LocalToForg[1]) != 0)
                            {
                                stats.TimesDeck1 += lingQ.TimesDeck_LocalToForg[0];
                                stats.TotalCountedDeck1++;
                            }
                        }
                        if (lingQ.TimesDeck_LocalToForg[2] > 0 && lingQ.TimesDeck_LocalToForg[1] > 0)
                        {
                            stats.TimesDeck2 += lingQ.TimesDeck_LocalToForg[1];
                            stats.TotalCountedDeck2++;
                        }
                        if (lingQ.TimesDeck_LocalToForg[3] > 0 && lingQ.TimesDeck_LocalToForg[2] > 0)
                        {
                            stats.TimesDeck3 += lingQ.TimesDeck_LocalToForg[2];
                            stats.TotalCountedDeck3++;
                        }
                        if (lingQ.TimesDeck_LocalToForg[4] > 0 && lingQ.TimesDeck_LocalToForg[3] > 0)
                        {
                            stats.TimesDeck4 += lingQ.TimesDeck_LocalToForg[3];
                            stats.TotalCountedDeck4++;
                        }
                        stats.TimesDeck5 += lingQ.TimesDeck_LocalToForg[4];
                        stats.TotalCards++;






                        for (int i = 0; i < 5; i++)
                        {
                            if (DateTime.Compare(lingQ.LatestDateDeck_LocalToForg[i], stats.LatestDate) > 0)
                            {
                                stats.LatestDate = lingQ.LatestDateDeck_LocalToForg[i];
                            }
                        }

                        if (lingQ.TimesDeck_LocalToForg[4] > 0)
                        {
                            stats.TotalInDeck5++;
                        }
                        else if (lingQ.TimesDeck_LocalToForg[3] > 0)
                        {
                            stats.TotalInDeck4++;
                        }
                        else if (lingQ.TimesDeck_LocalToForg[2] > 0)
                        {
                            stats.TotalInDeck3++;
                        }
                        else if (lingQ.TimesDeck_LocalToForg[1] > 0)
                        {
                            stats.TotalInDeck2++;
                        }
                        else if (lingQ.TimesDeck_LocalToForg[0] > 0)
                        {
                            stats.TotalInDeck1++;
                        }
                        else
                        {
                            stats.TotalInNoDeck++;
                        }


                    }
                    files.RemoveAt(0);
                }
                return stats;
            }
            return (null);
        }
        
        private void LoadCardsForToday(int ForceDeck)
        {
            //PresentList.Clear();
            //CurrentDeck_Ids.Clear();
            //CurrentDeck_Type.Clear();
            //DateTime EndOfDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
            //string[] filesstr = Directory.GetFiles(Global.PresentLibraryPath);
            //List<string> files = new List<string>(filesstr);
            //while (files.Count > 0)
            //{
            //    int ran = random.Next(0, files.Count);
            //    string path = files[ran];
            //    //File.Move(path, Global.PresentLibraryPath + "\\" + Path.GetFileName(path));
            //    LingQDesc lingQ = JsonConvert.DeserializeObject<LingQDesc>(File.ReadAllText(path));
            //    lingQ.CleanAddedSlots();
            //    lingQ.path = path;
            //    PresentList.Add(lingQ.id.ToString(), lingQ);
            //    if (CurrentDeck_Ids.Count < numericUpDownMaxCards.Value)
            //    {
            //            AddCard_ToDeck(true, lingQ, EndOfDay, -1, ForceDeck);
            //    }
            //    files.RemoveAt(ran);
            //}
            //if(CurrentDeck_Ids.Count > 0)
            //{
            //    CurrentDeck_CardIndex = 0;
            //    LoadCurrentCardInDeck(false);
            //}
            //else
            //{
            //    CurrentDeck_CardIndex = -1;
            //    HideAllCardElements();
            //    ShowButtonsAroundCard(false);
            //}
            
        }

        private void AddCard_ToDeck(bool randomLocation, LingQDesc lingQ,DateTime MaxDate, int DeckNum,int ForceDeckNum)
        {
            CurrentDeck_CardIndex++;
            int InsertionPoint = CurrentDeck_Ids.Count;
            if (( DateTime.Compare(lingQ.DateForgToLocal, MaxDate) <= 0) || lingQ.DeckForgToLocal == DeckNum)
            {
                if (ForceDeckNum < 0 || ForceDeckNum == lingQ.DeckForgToLocal)
                {
                    if (lingQ.StudyForgToLocal && checkBoxDeckForeignLocal.Checked)
                    {
                        if (randomLocation)
                        {
                            InsertionPoint = random.Next(0, InsertionPoint);
                        }
                        lingQ.DateForgToLocal = DateTime.Now;
                        CurrentDeck_Ids.Insert(InsertionPoint, lingQ.id.ToString());
                        CurrentDeck_Type.Insert(InsertionPoint, "DateForgToLocal");
                    }
                }
            }
            if (( DateTime.Compare(lingQ.DateLocalToForg, MaxDate) <= 0) || lingQ.DeckLocalToForg == DeckNum)
            {
                if (ForceDeckNum < 0 || ForceDeckNum == lingQ.DeckLocalToForg)
                {
                    if (lingQ.StudyLocalToForg && checkBoxDeckLocalForeign.Checked)
                    {
                        if (randomLocation)
                        {
                            InsertionPoint = random.Next(0, InsertionPoint);
                        }
                        lingQ.DateLocalToForg = DateTime.Now;
                        CurrentDeck_Ids.Insert(InsertionPoint, lingQ.id.ToString());
                        CurrentDeck_Type.Insert(InsertionPoint, "DateLocalToForg");
                    }
                }
            }
            if (( DateTime.Compare(lingQ.DateSoundToLocal, MaxDate) <= 0 ) || lingQ.DeckSoundToLocal == DeckNum)
            {
                if (ForceDeckNum < 0 || ForceDeckNum == lingQ.DeckSoundToLocal)
                {
                    if (lingQ.StudySoundToLocal && checkBoxDeckSoundLocal.Checked)
                    {
                        if (randomLocation)
                        {
                            InsertionPoint = random.Next(0, InsertionPoint);
                        }
                        lingQ.DateSoundToLocal = DateTime.Now;
                        CurrentDeck_Ids.Insert(InsertionPoint, lingQ.id.ToString());
                        CurrentDeck_Type.Insert(InsertionPoint, "DateSoundToLocal");
                    }
                }
            }
            if (( DateTime.Compare(lingQ.DateSoundToSpell, MaxDate) <= 0) || lingQ.DeckSoundToSpell == DeckNum)
            {
                if (ForceDeckNum < 0 || ForceDeckNum == lingQ.DeckSoundToSpell)
                {
                    if (lingQ.StudySoundToSpell && checkBoxDeckSoundSpelling.Checked)
                    {
                        if (randomLocation)
                        {
                            InsertionPoint = random.Next(0, InsertionPoint);
                        }
                        lingQ.DateSoundToSpell = DateTime.Now;
                        CurrentDeck_Ids.Insert(InsertionPoint, lingQ.id.ToString());
                        CurrentDeck_Type.Insert(InsertionPoint, "DateSoundToSpell");
                    }
                }
            }
        }

        private void butCardFlip_Click(object sender, EventArgs e)
        {
            labelCardBack.Visible = true;
            //labelCardBackFragment.Visible = true;
            //butCardFlip.Visible = false;

            labelCardFront.Visible = true;


            pictureBoxCardPicture.Visible = true;
            butCardShowPicture.Visible = false;

            PlaySound_OrM(true, -1,true);
            
            //if (LoadedCardSoundPath != "")
            //{
            //    wplayer.controls.play();
            //}
        }

        private void butCardShowPicture_Click(object sender, EventArgs e)
        {
            pictureBoxCardPicture.Visible = true;
            butCardShowPicture.Visible = false;
        }

        private void butCardPlayAgain_Click(object sender, EventArgs e)
        {
            if (LoadedCardSoundPath != "")
            {
                MediaPlayerSound = new DirectSoundOut(DeviceSound);
                audioFileSound = new AudioFileReader(LoadedCardSoundPath);
                audioFileSound.Volume = SoundVolume;
                MediaPlayerSound.Init(audioFileSound);
                MediaPlayerSound.Play();
            }
        }

        private void butCardNext_Click(object sender, EventArgs e)
        {
            CurrentDeck_CardIndex++;
            if (CurrentDeck_CardIndex >= CurrentDeck_Ids.Count)
            {
                CurrentDeck_CardIndex = 0;
            }
            LoadCurrentCardInDeck(false);
        }

        private void butCardPrevious_Click(object sender, EventArgs e)
        {
            CurrentDeck_CardIndex--;
            if (CurrentDeck_CardIndex < 0)
            {
                CurrentDeck_CardIndex = CurrentDeck_Ids.Count-1;
            }
            LoadCurrentCardInDeck(false);
        }

        private void butReloaddeck_Click(object sender, EventArgs e)
        {
            LoadCardsForToday(-1);
        }

        private void SaveCurrentCard()
        {
            if(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].StartedToLearnTime == DateTime.MinValue)
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].StartedToLearnTime = DateTime.Now;
            }
            File.WriteAllText(Global.PresentLibraryPath + "\\" + CurrentDeck_Ids[CurrentDeck_CardIndex],JsonConvert.SerializeObject(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]]));
        }
        private void butCardFinish_Click(object sender, EventArgs e)
        {
            if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateLocalToForg")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].StudyLocalToForg = false;
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateForgToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].StudyForgToLocal = false;
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].StudySoundToLocal = false;
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToSpell")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].StudySoundToSpell = false;
            }
            SaveCurrentCard();
            butCardNext_Click(null, null);
        }

        private void butDeck1_Click(object sender, EventArgs e)
        {
            
            if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateLocalToForg")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateLocalToForg = DateTime.Now.AddDays(1);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckLocalToForg = 1;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayLocalToForg = 1;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_LocalToForg[0],DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_LocalToForg[0] = DateTime.Today;
                }

                if(DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[0], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[0] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_LocalToForg[0]++;
                }

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[0], PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[1]) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[1] = DateTime.MinValue;
                    if(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_LocalToForg[1] > 0)
                    {
                        PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_LocalToForg[1]--;
                    }
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateForgToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateForgToLocal = DateTime.Now.AddDays(1);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckForgToLocal = 1;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayForgToLocal = 1;
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_ForgToLocal[0], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_ForgToLocal[0] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[0], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[0] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_ForgToLocal[0]++;
                }

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[0], PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[1]) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[1] = DateTime.MinValue;
                    if (PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_ForgToLocal[1] > 0)
                    {
                        PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_ForgToLocal[1]--;
                    }
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToLocal = DateTime.Now.AddDays(1);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToLocal = 1;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToLocal = 1;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_SoundToLocal[0], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_SoundToLocal[0] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[0], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[0] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_SoundToLocal[0]++;
                }

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[0], PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[1]) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[1] = DateTime.MinValue;
                    if (PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_SoundToLocal[1] > 0)
                    {
                        PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_SoundToLocal[1]--;
                    }
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToSpell")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToSpell = DateTime.Now.AddDays(1);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToSpell = 1;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToSpell = 1;
            }

            SaveCurrentCard();
            butCardNext_Click(null, null);
        }

        private void butDeck2_Click(object sender, EventArgs e)
        {
            
            if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateLocalToForg")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateLocalToForg = DateTime.Now.AddDays(3);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckLocalToForg = 2;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayLocalToForg = 3;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_LocalToForg[1], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_LocalToForg[1] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[1], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[1] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_LocalToForg[1]++;
                }

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[0], PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[1]) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[0] = DateTime.MinValue;
                    if (PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_LocalToForg[0] > 0)
                    {
                        PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_LocalToForg[0]--;
                    }
                }

            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateForgToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateForgToLocal = DateTime.Now.AddDays(3);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckForgToLocal = 2;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayForgToLocal = 3;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_ForgToLocal[1], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_ForgToLocal[1] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[1], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[1] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_ForgToLocal[1]++;
                }

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[0], PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[1]) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[0] = DateTime.MinValue;
                    if (PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_ForgToLocal[0] > 0)
                    {
                        PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_ForgToLocal[0]--;
                    }
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToLocal = DateTime.Now.AddDays(3);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToLocal = 2;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToLocal = 3;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_SoundToLocal[1], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_SoundToLocal[1] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[1], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[1] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_SoundToLocal[1]++;
                }

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[0], PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[1]) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[0] = DateTime.MinValue;
                    if (PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_SoundToLocal[0] > 0)
                    {
                        PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_SoundToLocal[0]--;
                    }
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToSpell")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToSpell = DateTime.Now.AddDays(3);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToSpell = 2;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToSpell = 3;
            }
            SaveCurrentCard();
            butCardNext_Click(null, null);
        }

        private void butDeck3_Click(object sender, EventArgs e)
        {
            if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateLocalToForg")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateLocalToForg = DateTime.Now.AddDays(7);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckLocalToForg = 3;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayLocalToForg = 7;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_LocalToForg[2], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_LocalToForg[2] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[2], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[2] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_LocalToForg[2]++;
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateForgToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateForgToLocal = DateTime.Now.AddDays(7);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckForgToLocal = 3;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayForgToLocal = 7;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_ForgToLocal[2], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_ForgToLocal[2] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[2], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[2] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_ForgToLocal[2]++;
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToLocal = DateTime.Now.AddDays(7);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToLocal = 3;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToLocal = 7;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_SoundToLocal[2], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_SoundToLocal[2] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[2], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[2] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_SoundToLocal[2]++;
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToSpell")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToSpell = DateTime.Now.AddDays(7);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToSpell = 3;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToSpell = 7;
            }
            SaveCurrentCard();
            butCardNext_Click(null, null);
        }

        private void butDeck4_Click(object sender, EventArgs e)
        {
            
            if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateLocalToForg")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateLocalToForg = DateTime.Now.AddDays(14);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckLocalToForg = 4;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayLocalToForg = 14;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_LocalToForg[3], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_LocalToForg[3] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[3], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[3] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_LocalToForg[3]++;
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateForgToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateForgToLocal = DateTime.Now.AddDays(14);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckForgToLocal = 4;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayForgToLocal = 14;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_ForgToLocal[3], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_ForgToLocal[3] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[3], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[3] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_ForgToLocal[3]++;
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToLocal = DateTime.Now.AddDays(14);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToLocal = 4;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToLocal = 14;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_SoundToLocal[3], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_SoundToLocal[3] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[3], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[3] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_SoundToLocal[3]++;
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToSpell")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToSpell = DateTime.Now.AddDays(14);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToSpell = 4;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToSpell = 14;
            }
            SaveCurrentCard();
            butCardNext_Click(null, null);
        }

        private void butDeck5_Click(object sender, EventArgs e)
        {
            
            if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateLocalToForg")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateLocalToForg = DateTime.Now.AddDays(30);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckLocalToForg = 5;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayLocalToForg = 30;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_LocalToForg[4], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_LocalToForg[4] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[4], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_LocalToForg[4] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_LocalToForg[4]++;
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateForgToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateForgToLocal = DateTime.Now.AddDays(30);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckForgToLocal = 5;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayForgToLocal = 30;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_ForgToLocal[4], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_ForgToLocal[4] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[4], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_ForgToLocal[4] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_ForgToLocal[4]++;
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToLocal = DateTime.Now.AddDays(30);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToLocal = 5;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToLocal = 30;

                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_SoundToLocal[4], DateTime.MinValue) == 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].FirstDateDeck_SoundToLocal[4] = DateTime.Today;
                }
                if (DateTime.Compare(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[4], DateTime.Today) != 0)
                {
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].LatestDateDeck_SoundToLocal[4] = DateTime.Today;
                    PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].TimesDeck_SoundToLocal[4]++;
                }
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToSpell")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToSpell = DateTime.Now.AddDays(30);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToSpell = 5;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToSpell = 30;
            }
            SaveCurrentCard();
            butCardNext_Click(null, null);
        }

        private void textBoxSpelling_TextChanged(object sender, EventArgs e)
        {
            if(textBoxSpelling.Text == PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].term)
            {
                textBoxSpelling.ForeColor = Color.DarkGreen;
            }
            else
            {
                textBoxSpelling.ForeColor = Color.DarkRed;
            }
        }

        private void butClearSound_Click(object sender, EventArgs e)
        {
            if(File.Exists(labelSound.Text))
            {
                MediaPlayerSound = null;
                audioFileSound = null;


                File.Delete(labelSound.Text);
            }
            labelSound.Text = "";
        }



        private void butStudyLater_Click(object sender, EventArgs e)
        {

        }

        private void butDeck5_60_Click(object sender, EventArgs e)
        {
            
            if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateLocalToForg")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateLocalToForg = DateTime.Now.AddDays(60);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckLocalToForg = 5;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayLocalToForg = 60;
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateForgToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateForgToLocal = DateTime.Now.AddDays(60);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckForgToLocal = 5;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayForgToLocal = 60;
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToLocal = DateTime.Now.AddDays(60);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToLocal = 5;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToLocal = 60;
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToSpell")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToSpell = DateTime.Now.AddDays(60);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToSpell = 5;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToSpell = 60;
            }
            SaveCurrentCard();
            butCardNext_Click(null, null);
        }

        private void butDeck5_180_Click(object sender, EventArgs e)
        {

        }

        private void checkBoxWithoutSpellings_CheckedChanged(object sender, EventArgs e)
        {

        }
        //List<WebRequest> requests = new List<WebRequest>();
        private void butCloseLingQ_Click(object sender, EventArgs e)
        {





        }

        private void butReviewDeck_Click(object sender, EventArgs e)
        {
            PresentList.Clear();
            CurrentDeck_Ids.Clear();
            CurrentDeck_Type.Clear();
            DateTime EndOfDay = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 23, 59, 59);
            EndOfDay = DateTime.MinValue;
            string[] filesstr = Directory.GetFiles(Global.PresentLibraryPath);
            List<string> files = new List<string>(filesstr);
            while (files.Count > 0)
            {
                int ran = random.Next(0, files.Count);
                string path = files[ran];
                File.Move(path, Global.PresentLibraryPath + "\\" + Path.GetFileName(path));
                LingQDesc lingQ = JsonConvert.DeserializeObject<LingQDesc>(File.ReadAllText(Global.PresentLibraryPath + "\\" + Path.GetFileName(path)));
                lingQ.CleanAddedSlots();
                PresentList.Add(lingQ.id.ToString(), lingQ);
                if (CurrentDeck_Ids.Count < numericUpDownMaxCards.Value)
                {
                    //AddCard_ToDeck(true, lingQ, EndOfDay, Convert.ToInt32(comboBoxDeckNumber.SelectedItem.ToString()),-1);
                }
                files.RemoveAt(ran);
            }
            if (CurrentDeck_Ids.Count > 0)
            {
                CurrentDeck_CardIndex = 0;
                LoadCurrentCardInDeck(false);
            }
            else
            {
                CurrentDeck_CardIndex = -1;
                HideAllCardElements();
                ShowButtonsAroundCard(false);
            }
        }

        private void butEditCurrentCard_Click(object sender, EventArgs e)
        {

        }

        private void labelCardBackFragment_Click(object sender, EventArgs e)
        {

        }

        private void labelCardFront_Click(object sender, EventArgs e)
        {

        }
        int NewCardIndex = 0;
        bool IsSlowPlay = false;
        private void butPlayCards_Click(object sender, EventArgs e)
        {
            //if (CurrentDeck_Ids.Count == 0)
            //{
            //    MessageBox.Show("Load Cards First");
            //}
            //else
            //{
            //    if (butPlayCardsSlow.Text == "Play Slow")
            //    {
            //        IsSlowPlay = true;
            //        timerPlay.Enabled = true;
            //        timerPlay.Interval = (int)numericUpDownPlayCardsDelay.Value * 1000;
            //        timerPlay.Stop();
            //        timerPlay.Start();
            //        if (checkBoxRedBlink.Checked)
            //        {
            //            timerRedBlink.Enabled = true;
            //            timerRedBlink.Interval = (int)numericUpDownRedBlink.Value;
            //        }
            //        butPlayCardsSlow.Text = "Stop Playing";
            //        NewCardIndex = CurrentDeck_CardIndex;
            //    }
            //    else
            //    {
            //        IsSlowPlay = false;
            //        timerRedBlink.Enabled = false;
            //        pictureBoxBackground.BackColor = Color.Transparent;
            //        butPlayCardsSlow.Text = "Play Slow";
            //        timerPlay.Enabled = false;
            //    }
            //}
            
        }
        bool GoBackwards = false;
        private void timerPlay_Tick(object sender, EventArgs e)
        {
            if (GoBackwards)
            {
                GoBackwards = false;
                CurrentDeck_CardIndex = CurrentDeck_CardIndex - (int)numericUpDownBackwardsEvery.Value;
                if(CurrentDeck_CardIndex < 0)
                {
                    CurrentDeck_CardIndex = 0;
                }
            }
            else
            {
                CurrentDeck_CardIndex++;
                if (CurrentDeck_CardIndex > NewCardIndex)
                {
                    NewCardIndex = CurrentDeck_CardIndex;
                    if (numericUpDownBackwardsEvery.Value > 0 && CurrentDeck_CardIndex >= (int)numericUpDownBackwardsEvery.Value)
                    {
                        GoBackwards = true;
                    }
                }
                if (CurrentDeck_CardIndex >= CurrentDeck_Ids.Count)
                {
                    CurrentDeck_CardIndex = 0;
                }
            }
            LoadCurrentCardInDeck(true);
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            CenterCardStuff();
        }

        private void tabPage1_Click(object sender, EventArgs e)
        {

        }

        private void labelCardBack_Click(object sender, EventArgs e)
        {

        }

        private void butDeck5_180_Click_1(object sender, EventArgs e)
        {
            if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateLocalToForg")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateLocalToForg = DateTime.Now.AddDays(365);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckLocalToForg = 5;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayLocalToForg = 365;
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateForgToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateForgToLocal = DateTime.Now.AddDays(365);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckForgToLocal = 5;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelayForgToLocal = 365;
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToLocal")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToLocal = DateTime.Now.AddDays(365);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToLocal = 5;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToLocal = 365;
            }
            else if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateSoundToSpell")
            {
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DateSoundToSpell = DateTime.Now.AddDays(365);
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DeckSoundToSpell = 5;
                PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]].DaysDelaySoundToSpell = 365;
            }
            SaveCurrentCard();
            butCardNext_Click(null, null);
        }
        bool IsRed = false;
        private void timerRedBlink_Tick(object sender, EventArgs e)
        {
            if(!IsRed)
            {
                //pictureBoxBackground.BackColor = Color.Red;
                IsRed = true;
            }
            else
            {
                IsRed = false;
                //pictureBoxBackground.BackColor = Color.Transparent;
            }
        }

        private void butCardsPlayFast_Click(object sender, EventArgs e)
        {
            //if (CurrentDeck_Ids.Count == 0)
            //{
            //    MessageBox.Show("Load Cards First");
            //}
            //else
            //{
            //    if (butCardsPlayFast.Text == "Play Fast")
            //    {
            //        IsSlowPlay = false;
            //        timerPlayFast.Enabled = true;
            //        timerPlayFast.Interval = (int)numericUpDownPlayCardsDelayFast.Value * 1000;
            //        timerPlayFast.Stop();
            //        timerPlayFast.Start();
            //        if (checkBoxRedBlink.Checked)
            //        {
            //            timerRedBlink.Enabled = true;
            //            timerRedBlink.Interval = (int)numericUpDownRedBlinkFast.Value;
            //        }
            //        butCardsPlayFast.Text = "Stop Playing";
            //        NewCardIndex = CurrentDeck_CardIndex;
            //    }
            //    else
            //    {
            //        IsSlowPlay = false;
            //        timerRedBlink.Enabled = false;
            //        pictureBoxBackground.BackColor = Color.Transparent;
            //        butCardsPlayFast.Text = "Play Fast";
            //        timerPlayFast.Enabled = false;
            //    }
            //}
        }

        private void timerPlayFast_Tick(object sender, EventArgs e)
        {
            if (GoBackwards)
            {
                GoBackwards = false;
                CurrentDeck_CardIndex = CurrentDeck_CardIndex - (int)numericUpDownBackwardsEveryFast.Value;
                if (CurrentDeck_CardIndex < 0)
                {
                    CurrentDeck_CardIndex = 0;
                }
            }
            else
            {
                CurrentDeck_CardIndex++;
                if (CurrentDeck_CardIndex > NewCardIndex)
                {
                    NewCardIndex = CurrentDeck_CardIndex;
                    if (numericUpDownBackwardsEveryFast.Value > 0 && CurrentDeck_CardIndex >= (int)numericUpDownBackwardsEveryFast.Value)
                    {
                        GoBackwards = true;
                    }
                }
                if (CurrentDeck_CardIndex >= CurrentDeck_Ids.Count)
                {
                    CurrentDeck_CardIndex = 0;
                }
            }
            LoadCurrentCardInDeck(true);
        }

        private void butCreateCard_SendLingQAsClosed_Click(object sender, EventArgs e)
        {


                File.Move(CurrentCreateCard.path, Global.DoneLibraryPath + "\\" + Path.GetFileName(CurrentCreateCard.path));
                FutureFiles.Remove(CurrentCreateCard);
                CreateNewCard_Click(null, null);



        }

        private void timerDelayTranslation_Tick(object sender, EventArgs e)
        {
            timerDelayTranslation.Enabled = false;
            textBoxTargetLang.Visible = true;
        }
        string CurrentMFile = "";
        private void comboBoxM_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (combo.Text.ToString() == " ")
            {
                Global.PresentLibraryPath = Global.PresentLibraryPathOriginal;
                //butPlayM.Visible = false;
                butCardPlayAgain.Visible = true;
                //checkBoxForceSound.Checked = true;
                //checkBoxForceSound.Visible = false;
                CurrentMFile = "";
                checkBoxDeckSoundLocal.Checked = true;
            }
            else
            {
                if (!Directory.Exists(Global.PresentLibraryPathOriginal + "\\" + combo.Text.ToString()))
                {
                    Directory.CreateDirectory(Global.PresentLibraryPathOriginal + "\\" + combo.Text.ToString());
                }
                Global.PresentLibraryPath = Global.PresentLibraryPathOriginal + "\\" + combo.Text.ToString();
                //butPlayM.Visible = true;
                butCardPlayAgain.Visible = true;
                //checkBoxForceSound.Checked = false;
                //checkBoxForceSound.Visible = true;
                checkBoxDeckSoundLocal.Checked = false;
                CurrentMFile = Global.CurrentCourseLibrary + "\\M\\" + combo.Text + ".mp3";

            }
            string[] files = Directory.GetFiles(Global.PresentLibraryPath);

            //labelTotalCards.Text = files.Length.ToString();

            LoadCardsForToday(-1);
        }

        private void butPlayM_Click(object sender, EventArgs e)
        {
            if (CurrentMFile != "")
            {
                MediaPlayerM = new DirectSoundOut(DeviceM);
                audioFileM = new AudioFileReader(CurrentMFile);
                MediaPlayerM.Init(audioFileM);
                MediaPlayerM.Play();
            }
        }

        private void butFlipCard2_Click(object sender, EventArgs e)
        {
            if (CurrentDeck_Type[CurrentDeck_CardIndex] == "DateLocalToForg")
            {
                labelCardBack.Visible = true;
            }
            //labelCardBackFragment.Visible = true;
            //butCardFlip.Visible = false;

            labelCardFront.Visible = true;


            pictureBoxCardPicture.Visible = true;
            butCardShowPicture.Visible = false;

            PlaySound_OrM(true, -1, true);

            //if (LoadedCardSoundPath != "")
            //{
            //    wplayer.controls.play();
            //}
        }

        private void butReloaddeck_forcedecknum_Click(object sender, EventArgs e)
        {
            //LoadCardsForToday(Convert.ToInt32(comboBoxDeckNumber.SelectedItem.ToString()));
        }

        private void butFlipFull_Click(object sender, EventArgs e)
        {
            labelCardBack.Visible = true;
            //labelCardBackFragment.Visible = true;
            //butCardFlip.Visible = false;

            labelCardFront.Visible = true;


            pictureBoxCardPicture.Visible = true;
            butCardShowPicture.Visible = false;

            PlaySound_OrM(true, -1, true);
        }

        private void deckToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void studyNewCardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string[] files = Directory.GetFiles(Global.ReadyLibraryPath);
            if (files.Length > 0)
            {
                string path = files[random.Next(0, files.Length)];

                File.Move(path, Global.PresentLibraryPath + "\\" + Path.GetFileName(path));

                LingQDesc lingQ = JsonConvert.DeserializeObject<LingQDesc>(File.ReadAllText(Global.PresentLibraryPath + "\\" + Path.GetFileName(path)));
                lingQ.Reset();
                if (lingQ.path == "")
                {
                    lingQ.path = Global.PresentLibraryPath + "\\" + Path.GetFileName(path);
                }
                File.WriteAllText(Global.PresentLibraryPath + "\\" + Path.GetFileName(path), JsonConvert.SerializeObject(lingQ));

                PresentList.Add(lingQ.id.ToString(), lingQ);
                AddCard_ToDeck(false, lingQ, DateTime.Now, -1, -1);
                LoadCurrentCardInDeck(false);

                string[] files1 = Directory.GetFiles(Global.PresentLibraryPath);

                //labelTotalCards.Text = files1.Length.ToString();
            }
            else
            {
                MessageBox.Show("Couldn't Find More Ready Cards");
            }
        }

        private void reviewDeck1ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadCardsForToday(1);
        }

        private void reviewDeck2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadCardsForToday(2);
        }

        private void reviewDeck3ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadCardsForToday(3);
        }

        private void reviewForTodayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadCardsForToday(-1);
        }

        private void currentToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void neverStudyCurrentCardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            File.Move(Global.PresentLibraryPath + "\\" + CurrentDeck_Ids[CurrentDeck_CardIndex], Global.DoneLibraryPath + "\\" + CurrentDeck_Ids[CurrentDeck_CardIndex]);
            PresentList.Remove(CurrentDeck_Ids[CurrentDeck_CardIndex]);
            string searchId = CurrentDeck_Ids[CurrentDeck_CardIndex];
            int indexFound = CurrentDeck_Ids.IndexOf(searchId);
            while (indexFound >= 0)
            {
                CurrentDeck_Ids.RemoveAt(indexFound);
                CurrentDeck_Type.RemoveAt(indexFound);
                indexFound = CurrentDeck_Ids.IndexOf(searchId);
                CurrentDeck_CardIndex--;
            }
            CurrentDeck_CardIndex++;
            if (CurrentDeck_CardIndex < 0)
            {
                CurrentDeck_CardIndex = 0;
            }
            else if (CurrentDeck_CardIndex > CurrentDeck_Ids.Count)
            {
                CurrentDeck_CardIndex = CurrentDeck_Ids.Count - 1;
            }

            LoadCurrentCardInDeck(false);
        }

        private void studyCurrentCardLaterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            File.Move(Global.PresentLibraryPath + "\\" + CurrentDeck_Ids[CurrentDeck_CardIndex], Global.ReadyLibraryPath + "\\" + CurrentDeck_Ids[CurrentDeck_CardIndex]);
            PresentList.Remove(CurrentDeck_Ids[CurrentDeck_CardIndex]);
            string searchId = CurrentDeck_Ids[CurrentDeck_CardIndex];
            int indexFound = CurrentDeck_Ids.IndexOf(searchId);
            while (indexFound >= 0)
            {
                CurrentDeck_Ids.RemoveAt(indexFound);
                CurrentDeck_Type.RemoveAt(indexFound);
                indexFound = CurrentDeck_Ids.IndexOf(searchId);
                CurrentDeck_CardIndex--;
            }



            LoadCurrentCardInDeck(false);
        }

        private void editCurrentCardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadCard_ToCreation(PresentList[CurrentDeck_Ids[CurrentDeck_CardIndex]]);


            pictureBoxCardPicture.ImageLocation = "";
            if (pictureBoxCardPicture.Image != null)
            {
                pictureBoxCardPicture.Image.Dispose();
                pictureBoxCardPicture.Image = null;
            }

            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        private async void button9_Click(object sender, EventArgs e)
        {
            textBoxAutoCreateInputText.Text = textBoxAutoCreateInputText.Text.Replace(".", "\r\n");
            if (comboBoxAutoCreateLangauge.SelectedItem != null)
            {
                string lang = comboBoxAutoCreateLangauge.SelectedItem.ToString();
                if (lang != "" && textBoxAutoCreateInputText.Text != "")
                {
                    string LanguagePrompt = "";
                    if(lang == "English")
                    {
                        LanguagePrompt = "Translate from English to russian ";

                    }
                    else
                    {
                        LanguagePrompt = "Translate from " + lang + " To English " ;
                    }
                    textBoxOut1.Text = await GetMessageFromGpt4Text(LanguagePrompt + PromptTranslate + textBoxAutoCreateInputText.Text,Gpt3Model);
                }
                else
                {
                    MessageBox.Show("Missing text");
                }
            }
            else
            {
                MessageBox.Show("Language not selected");
            }
        }

        private string OpenAI_TextToListOfWord(string txt)
        {


            return "";
        }

        private async Task<string> GetMessageFromGpt4Text(string message,string gptModel)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {OpenAIApiKey}");

                string apiUrl = "https://api.openai.com/v1/chat/completions";

                // Prepare request payload
                var payload = new
                {
                    model = gptModel,
                    messages = new List<object>
            {
                new
                {
                    role = "user",
                    content = message
                }
            },
                    //max_tokens = 300
                };

                // Serialize payload to JSON
                string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

                // Create StringContent with proper Content-Type
                StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Send request
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                // Process response
                if (response.IsSuccessStatusCode)
                {
                    var responseData = await response.Content.ReadAsStringAsync();
                    // Extract and return text response from API
                    dynamic jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject(responseData);
                    List<string> keywords = new List<string>();

                    string assistantMessage = GetAssistantContent(jsonResponse);
                    return assistantMessage.Replace("\n", "\r\n");
                }
                else
                {
                    // Handle error
                    MessageBox.Show($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return string.Empty;
                }
            }
        }



        private string GetAssistantContent(dynamic jsonResponse)
        {
            if (jsonResponse != null && jsonResponse.choices != null)
            {
                foreach (var choice in jsonResponse.choices)
                {
                    if (choice.message?.role == "assistant")
                    {
                        return choice.message.content?.ToString() ?? string.Empty;
                    }
                }
            }

            return string.Empty;
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            List<string> lines = new List<string>();
            foreach(string line in textBoxAutoCreateInputText.Lines)
            {
                if (line != "")
                {
                    lines.Add(line);
                }
            }
            for(int i = 0; i < textBoxOut1.Lines.Length; i++)
            {
                if (textBoxOut1.Lines[i] != "")
                {
                    lines[i] += "=" + textBoxOut1.Lines[i];
                }
            }
            string mergedString = string.Join(Environment.NewLine, lines);
            string prompt = PromptSplitSentence;
            if (comboBoxAutoCreateLangauge.SelectedItem != null)
            {
                string lang = comboBoxAutoCreateLangauge.SelectedItem.ToString();
                if (lang != "" && textBoxAutoCreateInputText.Text != "")
                {

                    if (lang == "English")
                    {
                        prompt = PromptSplitSentenceExamples_English.ToLower();
                    }
                    else if (lang == "Russian")
                    {
                        prompt = PromptSplitSentenceExamples_Russian.ToLower();
                    }
                    textBox2.Text = await GetMessageFromGpt4Text(prompt + mergedString,Gpt3Model);
                }
            }
            else
            {
                MessageBox.Show("Select Langauge");
            }
        }

        private void UpdateStatusLabel(string status)
        {
            if (labelStatus.InvokeRequired)
            {
                labelStatus.Invoke(new Action(() => labelStatus.Text = status));
            }
            else
            {
                labelStatus.Text = status;
            }
        }



        private void button8_Click(object sender, EventArgs e)
        {
            List<string> uniqueWords = ExtractUniqueWords(textBox2.Text);

            // Append each word from the uniqueWords list to the TextBox
            foreach (string word in uniqueWords)
            {
                textBox3.AppendText(word + Environment.NewLine); // Add word followed by a new line
            }
            
        }


        static List<string> ExtractUniqueWords(string text)
        {
            List<string> uniqueWords = new List<string>();
            string[] lines = text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string[] parts = line.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string part in parts)
                {
                    string[] subParts = part.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                    // Check if there's a word on the left side of '=' and add it to uniqueWords if it's not already present
                    if (subParts.Length > 0 && !uniqueWords.Contains(subParts[0]))
                    {
                        uniqueWords.Add(subParts[0]);
                    }
                }
            }

            return uniqueWords;
        }

        private async void butAutoCreateCardsProcess_Click(object sender, EventArgs e)
        {
            /////////////////////get translation 
            textBoxAutoCreateInputText.Text = CleanAndCapitalize(textBoxAutoCreateInputText.Text);
            string InText = textBoxAutoCreateInputText.Text;
            string[] Original_lines = textBoxAutoCreateInputText.Text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            string TranslationText = "";
            if (comboBoxAutoCreateLangauge.SelectedItem != null)
            {
                string lang = comboBoxAutoCreateLangauge.SelectedItem.ToString();
                if (lang != "" && textBoxAutoCreateInputText.Text != "")
                {
                    string LanguagePrompt = "";
                    if (lang == "English")
                    {
                        LanguagePrompt = "Translate from English to russian ";

                    }
                    else
                    {
                        LanguagePrompt = "Translate from " + lang + " To English ";
                    }
                    UpdateStatusLabel("Will Translate..");
                    TranslationText = await GetMessageFromGpt4Text(LanguagePrompt + PromptTranslate + textBoxAutoCreateInputText.Text,Gpt3Model);
                    TranslationText = TranslationText.ToLower();
                    UpdateStatusLabel("Translation Done");
                    textBoxOut1.Text = TranslationText;
                }
                else
                {
                    MessageBox.Show("Missing text");
                    return;
                }
            }
            else
            {
                MessageBox.Show("Language not selected");
                return;
            }

            /////////////////////combine original with translation and then split translation
            List<string> FullSentences_Translated = new List<string>();
            foreach (string line in Original_lines)
            {
                if (line != "")
                {
                    FullSentences_Translated.Add(line);
                }
            }
            string[] Translation_lines = TranslationText.Split('\n');
            for (int i = 0; i < Translation_lines.Length; i++)
            {
                if (Translation_lines[i] != "")
                {
                    FullSentences_Translated[i] += "=" + textBoxOut1.Lines[i];
                }
            }
            string mergedString = string.Join(Environment.NewLine, FullSentences_Translated);
            string prompt = PromptSplitSentence;
            string TranslatedSenteces_Splitted = "";
            if (comboBoxAutoCreateLangauge.SelectedItem != null)
            {
                string lang = comboBoxAutoCreateLangauge.SelectedItem.ToString();
                if (lang != "" && textBoxAutoCreateInputText.Text != "")
                {

                    if (lang == "English")
                    {
                        prompt = PromptSplitSentenceExamples_English.ToLower();
                    }
                    else if (lang == "Russian")
                    {
                        prompt = PromptSplitSentenceExamples_Russian.ToLower();
                    }
                    UpdateStatusLabel("Will Split Sentences..");
                    TranslatedSenteces_Splitted = await GetMessageFromGpt4Text(prompt + mergedString,Gpt3Model);
                    TranslatedSenteces_Splitted = TranslatedSenteces_Splitted.ToLower();
                    textBox2.Text=    TranslatedSenteces_Splitted;
                    UpdateStatusLabel("Done");
                }
            }
            else
            {
                MessageBox.Show("Select Langauge");
            }

            ///////////////////////  get list of words
            List<string> uniqueWords = ExtractUniqueWords(TranslatedSenteces_Splitted);

            // Append each word from the uniqueWords list to the TextBox
            foreach (string word in uniqueWords)
            {
                textBox3.AppendText(word + Environment.NewLine); // Add word followed by a new line
            }


        }


        static string CleanAndCapitalize(string inputText)
        {
            inputText = inputText.Replace(".", "\r\n");
            string[] lines = inputText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            string result = "";

            foreach (string line in lines)
            {
                string cleanedLine = CleanLine(line);
                string capitalizedLine = cleanedLine;// CapitalizeSentences(cleanedLine);
                result += capitalizedLine + "\r\n";
            }

            return result.Trim(); // Trim to remove trailing newline
        }

        static string CleanLine(string line)
        {
            // Remove leading and trailing whitespace
            string cleanedLine = line.Trim();

            // Remove unwanted characters from the beginning and end of the sentence
            cleanedLine = Regex.Replace(cleanedLine, @"^[,\.:""\s]+|[,\.:""\s]+$", "");
            cleanedLine = cleanedLine.ToLower();
            return cleanedLine;
        }

        private async void button10_Click(object sender, EventArgs e)
        {
            UpdateStatusLabel("Will get info..");
            textBox4.Text = await GetMessageFromGpt4Text(PromptWords_ToInfo + textBox3.Text,Gpt3Model);
            UpdateStatusLabel("got info..");
        }


        static Dictionary<string, List<string>> ExtractPartsOfSpeech(string input)
        {
            Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();

            string[] lines = input.Split('\n');

            foreach (string line in lines)
            {
                string[] parts = line.Split('|');
                string[] pos = parts[1].Split(',');

                foreach (string p in pos)
                {
                    string key = p.Trim();
                    if(key.ToLower().Contains("verb"))
                    {
                        key = "Verb";
                    }
                    if (!dictionary.ContainsKey(key))
                    {
                        dictionary[key] = new List<string>();
                    }
                    dictionary[key].Add(parts[0]);
                }
            }

            return dictionary;
        }

        private async void button11_Click(object sender, EventArgs e)
        {
            Dictionary<string, List<string>> partsOfSpeech = ExtractPartsOfSpeech(textBox4.Text);
            if(partsOfSpeech["Verb"].Count > 0)
            {   
                UpdateStatusLabel("Will get verb sentences..");
                textBox5.Text = await GetMessageFromGpt4Text(PromptVerbs_ToSentences + string.Join(Environment.NewLine, partsOfSpeech["Verb"]),Gpt4Model);
                UpdateStatusLabel("got it");
                
            }

        }

        private async void button12_Click(object sender, EventArgs e)
        {
            textBox5.Text = await GetMessageFromGpt4Text("use russian letters to write japanese. for example : Ogenki desu ka ? will be огенки десу ка ? write these sentences, first translate them to japanese.then write the japanese in russian. write me only the russian.no explanations.translate this: how are you, i am ok,this is good,this is bad", Gpt3Model);
        }

        private async void buttontranslatesentence_Click(object sender, EventArgs e)
        {
            UpdateStatusLabel("Will get info..");
            
            textBox6.Text = await GetMessageFromGpt4Text(PromptTranslateSentencesAllLangauges + DivideSentencesToString(textBox5.Text), Gpt4Model);
            UpdateStatusLabel("got info..");
        }

        public static string DivideSentencesToString(string input)
        {
            StringBuilder result = new StringBuilder();

            // Split input by newline character
            string[] lines = input.Split('\n');

            foreach (string line in lines)
            {
                // Split each line by '|' character
                string[] sentences = line.Split('|');

                foreach (string sentence in sentences)
                {
                    // Trim and append each sentence to result with a newline
                    result.AppendLine(sentence.Trim());
                }
            }

            return result.ToString();
        }

        private async void buttonBreakSentences_Click(object sender, EventArgs e)
        {
            textBox7.Text = await GetMessageFromGpt4Text(PromptSplitSentenceGeneral + PromptSplitSentenceExamples_English + textBox6.Text, Gpt4Model);
        }

        private async void button13_Click(object sender, EventArgs e)
        {
            textBox7.Text = await GetMessageFromGpt4Text(PromptTranslateWordsAllLangauges + textBox6.Text, Gpt4Model);
        }
        //static string CapitalizeSentences(string line)
        //{
        //    //TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

        //    //// Capitalize the first letter of each sentence
        //    //string capitalizedLine = textInfo.ToTitleCase(line);

        //    //return capitalizedLine;
        //}


    }


    public class Hint
    {
        public int id { get; set; }
        public string locale { get; set; }
        public string text { get; set; }
        public int popularity { get; set; }
    }

    public class Statistics_Of_Lib
    {
        public double TotalCountedDeck1 = 0;
        public double TotalCountedDeck2 = 0;
        public double TotalCountedDeck3 = 0;
        public double TotalCountedDeck4 = 0;
        public double TotalCountedDeck5 = 0;

        public int TotalCards = 0;

        public double TimesDeck1 = 0;
        public double TimesDeck2 = 0;
        public double TimesDeck3 = 0;
        public double TimesDeck4 = 0;
        public double TimesDeck5 = 0;

        public DateTime LatestDate = new DateTime();
        public double TotalInNoDeck = 0;
        public double TotalInDeck1 = 0;
        public double TotalInDeck2 = 0;
        public double TotalInDeck3 = 0;
        public double TotalInDeck4 = 0;
        public double TotalInDeck5 = 0;
    }


    public class LingQDesc
    {
        public LingQDesc()
        {
            if (TimesDeck_ForgToLocal.Count == 0)
            {
                for (int i = 0; i < 5; i++)
                {
                    TimesDeck_ForgToLocal.Add(0);
                    TimesDeck_LocalToForg.Add(0);
                    TimesDeck_SoundToLocal.Add(0);
                    TimesDeck_SoundToSpell.Add(0);

                    LatestDateDeck_ForgToLocal.Add(new DateTime());
                    LatestDateDeck_LocalToForg.Add(new DateTime());
                    LatestDateDeck_SoundToLocal.Add(new DateTime());
                    LatestDateDeck_SoundToSpell.Add(new DateTime());

                    FirstDateDeck_ForgToLocal.Add(new DateTime());
                    FirstDateDeck_LocalToForg.Add(new DateTime());
                    FirstDateDeck_SoundToLocal.Add(new DateTime());
                    FirstDateDeck_SoundToSpell.Add(new DateTime());
                }
            }

        }

        public void CleanAddedSlots()
            {
            while (TimesDeck_ForgToLocal.Count > 5)
            {
                TimesDeck_ForgToLocal.RemoveAt(0);
                TimesDeck_LocalToForg.RemoveAt(0);
                TimesDeck_SoundToLocal.RemoveAt(0);
                TimesDeck_SoundToSpell.RemoveAt(0);

                LatestDateDeck_ForgToLocal.RemoveAt(0);
                LatestDateDeck_LocalToForg.RemoveAt(0);
                LatestDateDeck_SoundToLocal.RemoveAt(0);
                LatestDateDeck_SoundToSpell.RemoveAt(0);

                FirstDateDeck_ForgToLocal.RemoveAt(0);
                FirstDateDeck_LocalToForg.RemoveAt(0);
                FirstDateDeck_SoundToLocal.RemoveAt(0);
                FirstDateDeck_SoundToSpell.RemoveAt(0);
            }
        }
        public void Reset()
        {
            TimesDeck_ForgToLocal.Clear();
            TimesDeck_LocalToForg.Clear();
            TimesDeck_SoundToLocal.Clear();
            TimesDeck_SoundToSpell.Clear();

            LatestDateDeck_ForgToLocal.Clear();
            LatestDateDeck_LocalToForg.Clear();
            LatestDateDeck_SoundToLocal.Clear();
            LatestDateDeck_SoundToSpell.Clear();

            FirstDateDeck_ForgToLocal.Clear();
            FirstDateDeck_LocalToForg.Clear();
            FirstDateDeck_SoundToLocal.Clear();
            FirstDateDeck_SoundToSpell.Clear();

            for (int i = 0; i < 5; i++)
            {
                TimesDeck_ForgToLocal.Add(0);
                TimesDeck_LocalToForg.Add(0);
                TimesDeck_SoundToLocal.Add(0);
                TimesDeck_SoundToSpell.Add(0);

                LatestDateDeck_ForgToLocal.Add(new DateTime());
                LatestDateDeck_LocalToForg.Add(new DateTime());
                LatestDateDeck_SoundToLocal.Add(new DateTime());
                LatestDateDeck_SoundToSpell.Add(new DateTime());

                FirstDateDeck_ForgToLocal.Add(new DateTime());
                FirstDateDeck_LocalToForg.Add(new DateTime());
                FirstDateDeck_SoundToLocal.Add(new DateTime());
                FirstDateDeck_SoundToSpell.Add(new DateTime());
            }


            DateForgToLocal = new DateTime();
            DateLocalToForg = new DateTime();
            DateSoundToLocal = new DateTime();
            DateSoundToSpell = new DateTime();

            LastShowForgToLocal = new DateTime();
            LastShowLocalToForg = new DateTime();
            LastShowSoundToLocal = new DateTime();
            LastShowSoundToSpell = new DateTime();

            StartedToLearnTime = new DateTime();
            DeckForgToLocal = 1;
            DeckLocalToForg = 1;
            DeckSoundToLocal = 1;
            DeckSoundToSpell = 1;


        }
        public int id { get; set; }
        public string term { get; set; }
        public int word { get; set; }
        public List<Hint> hints { get; set; }
        public string fragment { get; set; }
        public int status { get; set; }
        public int? extended_status { get; set; }
        public string notes { get; set; }
        public List<object> tags { get; set; }
        public bool StudyForgToLocal = true;
        public bool StudyLocalToForg = true;
        public bool StudySoundToLocal = true;
        public bool StudySoundToSpell = true;


        public DateTime DateForgToLocal = new DateTime();
        public DateTime DateLocalToForg = new DateTime();
        public DateTime DateSoundToLocal = new DateTime();
        public DateTime DateSoundToSpell = new DateTime();

        public DateTime LastShowForgToLocal = new DateTime();
        public DateTime LastShowLocalToForg = new DateTime();
        public DateTime LastShowSoundToLocal = new DateTime();
        public DateTime LastShowSoundToSpell = new DateTime();

        public DateTime StartedToLearnTime = new DateTime();
        public int DeckForgToLocal = 1;
        public int DeckLocalToForg = 1;
        public int DeckSoundToLocal = 1;
        public int DeckSoundToSpell = 1;

        public int DaysDelayForgToLocal = 1;
        public int DaysDelayLocalToForg = 1;
        public int DaysDelaySoundToLocal = 1;
        public int DaysDelaySoundToSpell = 1;


        public List<int> TimesDeck_ForgToLocal = new List<int>();
        public List<int> TimesDeck_LocalToForg = new List<int>();
        public List<int> TimesDeck_SoundToLocal = new List<int>();
        public List<int> TimesDeck_SoundToSpell = new List<int>();

        public List<DateTime> LatestDateDeck_ForgToLocal = new List<DateTime>();
        public List<DateTime> LatestDateDeck_LocalToForg = new List<DateTime>();
        public List<DateTime> LatestDateDeck_SoundToLocal = new List<DateTime>();
        public List<DateTime> LatestDateDeck_SoundToSpell = new List<DateTime>();

        public List<DateTime> FirstDateDeck_ForgToLocal = new List<DateTime>();
        public List<DateTime> FirstDateDeck_LocalToForg = new List<DateTime>();
        public List<DateTime> FirstDateDeck_SoundToLocal = new List<DateTime>();
        public List<DateTime> FirstDateDeck_SoundToSpell = new List<DateTime>();

        public string LastSaveLocation = "";
        public string path = "";
    }

    public class CoursesDesc
    {
        public string code { get; set; }
        public string title { get; set; }
        public bool supported { get; set; }
    }
    public static class Settings
    {
        public static bool checkBoxWithoutSpellings = false;
        public static bool checkBoxWithoutSoundToLocal = false;
        public static int numericUpDownMaxCards = 50;
    }

    public class Word
    {
        public string Lang = "";
        public Dictionary<string, string> Lang_Translation = new Dictionary<string, string>();
        public Dictionary<string, List<Sentence>> Sentences = new Dictionary<string, List<Sentence>>();
        public string BaseWord = "";
        public int Level = 0;
    }

    public class Sentence
    {
        public Dictionary<string, SentenceDesc> Lang_Translation = new Dictionary<string, string>();
    }

    public class SentenceDesc
    {
        public string Sentence = "";
        public Dictionary<string,string> Lang_Words = new Dictionary<string,string>();
        public int WordsNum = 0;
    }

    public static class Global
    {
        public static string CurrentCourseLibrary = "";
        public static string CoursesLibrary = "Courses";

        public static string PicturesLibrary = "Pictures";
        public static string PicturesLibraryPath = "";
        public static string SoundsLibrary = "Sounds";
        public static string SoundsLibraryPath = "";

        public static string DaysLibrary = "Days";
        public static string DaysLibraryPath = "";
        public static string DecksLibrary = "Decks";
        public static string DecksLibraryPath = "";

        //public static string FutureLibrary = "CardsFuture";
        //public static string FutureLibraryPath = "";
        //public static string FutureDelayedLibrary = "CardsFutureDelayed";
        //public static string FutureDelayedLibraryPath = "";
        public static string PresentLibrary = "CardsPresent";
        public static string PresentLibraryPath = "";
        public static string PresentLibraryPathOriginal = "";
        public static string ReadyLibrary = "CardsReady";
        public static string ReadyLibraryPath = "";
        public static string DoneLibrary = "CardsDone";
        public static string DoneLibraryPath = "";
        public static string NeverLibrary = "CardsNeverStudy";
        public static string NeverLibraryPath = "";



    }
}
