using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using System.Security.Policy;
using System.Reflection;

namespace DailyMTGImageSpider
{
    class Program
    {
        public const string STARTING_URL = "http://magic.wizards.com/en/articles";
        public const string ROOT_URL = "http://magic.wizards.com";

        public const string OUTPUT_FOLDER = "C:\\Users\\Liam\\Desktop\\output";

        public List<string> openList;
        public List<string> closedList;
        public string executionPath;
        public bool isCancelling = false;

        static void Main( string[] _args )
        {
            Program program = new Program( _args );
            program.Run();
        }

        public Program( string[] _args )
        {
            openList = new List<string>();
            closedList = new List<string>();

            executionPath = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );

            this.ReadListFromFIle( ref this.openList, "openlist" );
            this.ReadListFromFIle( ref this.closedList, "closedlist" );

            if ( this.openList.Count == 0 )
            {
                this.openList.Add( STARTING_URL );
            }
        }

        public void Run()
        {
            Console.Clear();
            Console.TreatControlCAsInput = false;
            Console.CancelKeyPress += this.OnCancelKeyPress;

            while ( this.openList.Count > 0 )
            {
                if ( this.isCancelling )
                {
                    this.WriteListToFile( this.openList, "openlist" );
                    this.WriteListToFile( this.closedList, "closedlist" );
                    return;
                }

                string url = openList[0];
                openList.RemoveAt( 0 );
                closedList.Add( url );

                if ( url[0] == '/' )
                {
                    url = ROOT_URL + url;
                }

                Console.WriteLine( "Parsing {0}", url );

                WebClient webClient = new WebClient();
                string filedata;
                try
                {
                   filedata = webClient.DownloadString( url );
                }
                catch ( System.Net.WebException )
                {
                    continue;
                }
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml( filedata );

                HtmlNodeCollection backgroundNodes = doc.DocumentNode.SelectNodes( "//*[contains( @style, 'background-image' )]" );

                HtmlNodeCollection imageNodes = doc.DocumentNode.SelectNodes( "//img" );
                if ( imageNodes != null )
                {
                    foreach ( HtmlNode node in imageNodes )
                    {
                        if ( node.Attributes["src"] == null || node.Attributes["src"].Value.Length == 0 )
                        {
                            continue;
                        }

                        string imagePath = node.Attributes["src"].Value;
                        if ( imagePath[0] == '/' )
                        {
                            imagePath = ROOT_URL + imagePath;
                        }

                        Uri imageUrl;
                        try
                        {
                            imageUrl = new Uri( imagePath );
                        }
                        catch ( System.UriFormatException )
                        {
                            continue;
                        }

                        Uri fileUri = new Uri( OUTPUT_FOLDER + imageUrl.LocalPath );
                        Uri dirUri = SnipLastUriElement( fileUri );
                        try
                        {
                            Directory.CreateDirectory( dirUri.LocalPath );
                        }
                        catch
                        {
                            continue;
                        }

                        if ( !File.Exists( fileUri.LocalPath ) )
                        {
                            Console.WriteLine( "Downloading {0}", imageUrl );
                            WebClient imageClient = new WebClient();
                            try
                            {
                                imageClient.DownloadFile( imageUrl, fileUri.LocalPath );
                            }
                            catch ( System.Net.WebException )
                            {
                                Console.WriteLine( "Failed {0}", imageUrl );
                                continue;
                            }
                        }
                    }
                }
                HtmlNodeCollection linkNodes = doc.DocumentNode.SelectNodes( "//a" );
                if ( linkNodes != null )
                {
                    foreach ( HtmlNode node in linkNodes )
                    {
                        if ( node.Attributes["href"] == null )
                        {
                            continue;
                        }

                        string linkUrl = node.Attributes["href"].Value;
                        if ( !closedList.Contains( linkUrl )
                          && !openList.Contains( linkUrl )
                          && ( linkUrl[0] == '/' || linkUrl.Contains( "wizards.com" ) )
                          && !linkUrl.Contains( '?' )
                          && !linkUrl.Contains( "dnd.wizards.com" )
                          && !linkUrl.EndsWith( ".jpg" )
                          && !linkUrl.EndsWith( ".png" )
                          && !linkUrl.EndsWith( ".gif" ) )
                        {
                            openList.Add( linkUrl );
                            Console.WriteLine( "Adding {0}", linkUrl );
                        }
                    }
                }
            }
            Console.WriteLine( "Done" );
        }

        public void WriteListToFile( List<string> _list, string _filename )
        {
            StreamWriter outStream = new StreamWriter( _filename );
            foreach ( string str in _list )
            {
                outStream.WriteLine( str );
            }
            outStream.Close();
        }

        public void ReadListFromFIle( ref List<string> _list, string _filename )
        {
            StreamReader inStream = new StreamReader( _filename );
            string str;
            while ( (str = inStream.ReadLine()) != null )
            {
                _list.Add( str );
            }
            inStream.Close();
        }

        public static Uri SnipLastUriElement( Uri _uri )
        {
            string output = "";
            for ( int i = 1; i < _uri.Segments.Length - 1; i++ )
            {
                output += _uri.Segments[i];
            }
            return new Uri( output );
        }

        public void OnCancelKeyPress( object _sender, ConsoleCancelEventArgs _e )
        {
            _e.Cancel = true;
            this.isCancelling = true;
        }
    }
}
