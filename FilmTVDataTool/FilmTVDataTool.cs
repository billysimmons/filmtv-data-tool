using System.Globalization;
using System.Text.Json;
using CsvHelper;

namespace FilmTVDataTool
{
    public class SearchResultData
    {
        public List<Item>? Search { get; set; }
        public string? TotalResults { get; set; }
        public string? Response { get; set; }
    }

    public class Item
    {
        public string? Title { get; set; }
        public string? Year { get; set; }
        public string? Actors { get; set; }
        public string? Plot { get; set; }
        public string? Language { get; set; }
        public string? Country { get; set; }
        public string? Awards { get; set; }
        public string? Poster { get; set; }
        public List<ItemRating>? Ratings { get; set; }
        public string? Metascore { get; set; }
        public string? imdbRating { get; set; }
        public string? imdbVotes { get; set; }
        public string? imdbID { get; set; }
        public string? Type { get; set; }
        public string? DVD { get; set; }
        public string? BoxOffice { get; set; }
        public string? Production { get; set; }
        public string? Website { get; set; }
        public string? Response { get; set; }
    }

    public class ItemRating
    {
        public string? Source { get; set; }
        public string? Value { get; set; }
    }

    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly string _key;

        public ApiClient(string baseUrl, string key)
        {
            _httpClient = new HttpClient();
            _baseUrl = baseUrl;
            _key = key;
        }

        public async Task<string> Get(string value, string searchType, string mediaType, int? page)
        {
            string searchTypeIdentifier;

            switch (searchType)
            {
                case "search":
                    searchTypeIdentifier = "s";
                    break;
                case "title":
                    searchTypeIdentifier = "t";
                    break;
                default:
                    searchTypeIdentifier = "";
                    break;
            }

            string url;

            if (page != null)
            {
                url = $"{_baseUrl}{searchTypeIdentifier}={value}&apikey={_key}&type={mediaType}&page={page}";
            }
            else
            {
                url = $"{_baseUrl}{searchTypeIdentifier}={value}&apikey={_key}&type={mediaType}&";
            }

            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception($"Failed to fetch data from {url}. Status code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while fetching data from {url}: {ex.Message}", ex);
            }
        }
    }

    public class ConsoleSelector
    {
        public static T Selector<T>(List<T> selectorItems, string prompt)
        {
            Console.CursorVisible = false;
            int selectedIndex = 1;

            while (true)
            {
                Console.Clear();

                Console.WriteLine(prompt);

                for (int i = 0; i < selectorItems.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.Write("-> ");
                        Console.BackgroundColor = ConsoleColor.Blue;
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(selectorItems[i]);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.WriteLine($"   {selectorItems[i]}");
                    }
                }

                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.UpArrow:
                        selectedIndex = Math.Max(0, selectedIndex - 1);
                        break;
                    case ConsoleKey.DownArrow:
                        selectedIndex = Math.Min(selectorItems.Count - 1, selectedIndex + 1);
                        break;
                    case ConsoleKey.Enter:
                        Console.Clear();
                        return selectorItems[selectedIndex];
                }
            }
        }
    }

    public class Program
    {
        static async Task Main()
        {
            string apiKey = "486ae419";
            string baseUrl = $"http://www.omdbapi.com/?";
            int maxPages = 2;

            ApiClient apiClient = new ApiClient(baseUrl, apiKey);

            List<Item> items = new List<Item>();

            bool quit = false;
            while (!quit)
            {
                List<string> selectorItems = new List<string>();

                Console.Clear();
                Console.Write("Enter type (Movie/Series): ");
                string mediaType = Console.ReadLine();

                Console.Write("Enter name of Movie/Series: ");
                string searchTerm = Console.ReadLine();

                Console.WriteLine();

                try
                {
                    if (searchTerm != null && mediaType != null)
                    {
                        mediaType.ToLower();
                        searchTerm.ToLower();
                        int resultIndex = 1;

                        for (int i = 1; i <= maxPages; i++)
                        {
                            string searchResponseData = await apiClient.Get(searchTerm, "search", mediaType, i);

                            var searchResult = JsonSerializer.Deserialize<SearchResultData>(searchResponseData);

                            if (searchResult != null && searchResult.Search != null)
                            {
                                foreach (var result in searchResult.Search)
                                {
                                    if (result != null && result.Title != null)
                                    {
                                        selectorItems.Add(result.Title);
                                        resultIndex++;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }

                if (mediaType != null)
                {
                    var selectedItem = ConsoleSelector.Selector(selectorItems, $"Select the {mediaType} you wish to add: ");

                    string titleResponseData = await apiClient.Get(selectedItem, "title", mediaType, null);
                    Item? newItem = JsonSerializer.Deserialize<Item>(titleResponseData);

                    if (newItem != null)
                    {
                        items.Add(newItem);
                    }
                }

                List<string> yesOrNo = new List<string> { "yes", "no" };

                var willContinue = ConsoleSelector.Selector(yesOrNo, $"Would you like to select another {mediaType}?: ");

                if (willContinue != "yes")
                {

                    Console.WriteLine("A csv file containing your list of movies will now be generated. Press any key to continue: ");
                    Console.ReadKey();

                    using (var writer = new StreamWriter("movie-list.csv"))

                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        csv.WriteRecords(items);
                    }

                    quit = true;
                }
            }
        }
    }
}

