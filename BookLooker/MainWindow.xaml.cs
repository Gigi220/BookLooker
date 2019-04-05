using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Net;
using System.Data.SqlClient;
using System.Data.Entity;
using HtmlAgilityPack;
using System.IO;


namespace BookLooker
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	/// 

	public class Book
	{
		public int ID { get; set; }
		public string Auther { get; set; }
		public string Name { get; set; }
		public string ShortTitle { get; set; }
		public string ImgPath { get; set; }
		//public string DownloadLink { get; set; }

		public Book()
		{
			ID = 0;
			Name = Auther = ShortTitle = ImgPath = /*DownloadLink =*/ "";
		}
		public Book(string auther, string name, string shortTitle, string imgPath) 
		{
			Auther = auther;
			Name = name;
			ShortTitle = shortTitle;
			ImgPath = imgPath;
			//DownloadLink = downloadLink;
		}
	}

	public class BookContext : DbContext
	{
		public BookContext() : base("DefaultConnection")
		{ }

		public DbSet<Book> Books { get; set; }
	}

	public partial class MainWindow : Window
	{
		public static BookContext db;
		public static HtmlWeb htmlWeb = new HtmlWeb();
		public static WebClient webClient = new WebClient();

		public MainWindow()
		{
			InitializeComponent();

			BooksListView.SelectionChanged += delegate (object sender, SelectionChangedEventArgs e)
			{
				if(BooksListView.SelectedItem != null)
				{
					BookNameLabel.Content = ((Book)((Border)BooksListView.SelectedItem).Tag).Name;
					BookAutherLabel.Content = ((Book)((Border)BooksListView.SelectedItem).Tag).Auther;

					ImageInfo.Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath(((Book)((Border)BooksListView.SelectedItem).Tag).ImgPath)));

					BookShortInfoLabel.Text = ((Book)((Border)BooksListView.SelectedItem).Tag).ShortTitle;
				}
			};

		}

		public static void Find(string URL)
		{
			try
			{
				var doc = htmlWeb.Load(URL);

				var anchors = doc.DocumentNode.Descendants("a").Where(anchor => anchor.GetClasses().Contains("btn"));

				foreach(var anchor in anchors)
				{
					doc = htmlWeb.Load(anchor.Attributes.First(p => p.Name == "href").Value);

					var AutherNodes = doc.DocumentNode.Descendants("span");
					string auther = "";
					foreach (var x in AutherNodes)
					{
						foreach(var att in x.Attributes)
						{
							if(att.Name == "itemprop" && att.Value == "title")
							{
								auther = x.InnerText;
							}
						}
					}

					var NameNode = doc.DocumentNode.Descendants("h1").First(p => p.Attributes.First(att => att.Name == "itemprop" && att.Value == "name") != null);
					string name = NameNode.InnerText;

					var imgNode = doc.DocumentNode.Descendants("img").First(p => p.Attributes.First(att => att.Name == "itemprop" && att.Value == "image") != null);
					string imgPath = "";
					foreach (var x in imgNode.Attributes)
					{
						if(x.Name == "src")
						{
							string path = @"...\...\Images\" + System.IO.Path.GetFileName(x.Value);
							webClient.DownloadFile(x.Value, path);
							imgPath = path;
						}
					}

					//string downloadLink = "";
					//var LinkNode = doc.DocumentNode.Descendants("a").First(p => p.Attributes.First(att => att.Name == "data-format" && att.Value == "txt") != null);
					//downloadLink = LinkNode.Attributes["href"].Value;

					var DivNode = doc.DocumentNode.Descendants("div").First(p => p.GetClasses().Contains("wrap_description"));
					string shortTitle = "";
					foreach(var x in DivNode.ChildNodes)
					{
						if(x.Name == "p")
						{
							shortTitle += x.InnerText;
						}
					}

					AddBook(auther, name, shortTitle, imgPath);

				}
			}
			catch(Exception e)
			{
				MessageBox.Show(e.Message);
			}
		}

		public void DownloadTXT(string id)
		{
			HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(@"http://avidreaders.ru/api/get.php?b=" + id + "&f=txt");
			httpWebRequest.Referer = "http://avidreaders.ru/";

			HttpWebResponse response = (HttpWebResponse)httpWebRequest.GetResponse();

			webClient.DownloadFile(response.ResponseUri, @"..\..\BooksTXT\" + id + ".zip");
			response.Close();
		}

		public void GenerateListView()
		{
			using (db = new BookContext())
			{
				foreach (var Book in db.Books)
				{
					Book book = Book;
					Border newItem = new Border();
					newItem.Margin = new Thickness(0, 0, 0, 10);

					Grid grid = new Grid();
					grid.Background = new SolidColorBrush(Color.FromRgb(233, 233, 233));
					grid.Height = 150;
					grid.Width = 280;
					grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
					grid.ColumnDefinitions.Add(new ColumnDefinition());

					Button button = new Button { Content = new Image { Source = new BitmapImage(new Uri(System.IO.Path.GetFullPath(Book.ImgPath))) } };
					grid.Children.Add(button);
					Grid.SetColumn(button, 0);

					TextBlock textBlock = new TextBlock { Text = Book.Name, Foreground = new SolidColorBrush(Color.FromRgb(37, 28, 28)), FontSize = 18, FontFamily = new FontFamily("Comic Sans MS"), TextWrapping = TextWrapping.Wrap };
					textBlock.Margin = new Thickness(5, 5, 5, 5);
					grid.Children.Add(textBlock);
					Grid.SetColumn(textBlock, 1);

					newItem.Child = grid;

					newItem.Tag = book;

					BooksListView.Items.Add(newItem);
				}
			}
		}

		public static void AddBook(string auther, string name, string shortTitle, string imgPath)
		{
			using (db = new BookContext())
			{
				Book newBook = new Book(auther, name, shortTitle, imgPath);

				db.Books.Add(newBook);
				db.SaveChanges();
			}
		}

		private void ImageButton_Click(object sender, RoutedEventArgs e)
		{
			DownloadTXT(System.IO.Path.GetFileNameWithoutExtension(((Image)((Button)sender).Content).Source.ToString()));
			MessageBox.Show("Has been downloaded");
		}

		private void ReloadButton_Click(object sender, RoutedEventArgs e)
		{
			if(BooksListView.Items.Count != 0)
			{
				BooksListView.Items.Clear();
			}
			GenerateListView();
		}

		private void FindButton_Click(object sender, RoutedEventArgs e)
		{
			Task.WaitAll();
			Task.Run(() => Find(@"http://avidreaders.ru/books/index.html"));
		}
	}
}
