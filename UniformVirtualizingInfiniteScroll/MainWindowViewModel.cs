using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UniformVirtualizingInfiniteScroll
{
    internal class MainWindowViewModel : ObservableObject
    {
        private HttpClient _httpClient = new();
        private IAsyncEnumerator<Joke> _jokeEnumerator;
        private bool _isLoading;

        private bool _loadCompleted;
        public bool LoadCompleted
        {
            get => this._loadCompleted;
            set => this.SetProperty(ref this._loadCompleted, value);
        }
        private bool _hasError;
        public bool HasError
        {
            get => this._hasError;
            set => this.SetProperty(ref this._hasError, value);
        }
        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => this._errorMessage;
            set => this.SetProperty(ref this._errorMessage, value);
        }
        public ICommand LoadCommand { get; }

        public ICommand MoveUpCommand { get; }
        public ICommand MoveDownCommand { get; }
        public ICommand UpdateCommand { get; }
        public ICommand RemoveCommand { get; }
        public ICommand ClearCommand { get; }

        public ObservableCollection<Joke> Jokes { get; } = new();

        public MainWindowViewModel()
        {
            this.LoadCommand = new AsyncRelayCommand(this.AddJoke, this.AddJokeAvailable);
            this._jokeEnumerator = this.Get10Jokes();
            this.MoveUpCommand = new RelayCommand<Joke>(joke =>
            {
                if (joke == null) { return; }
                var index = this.Jokes.IndexOf(joke);
                if (index > 0)
                {
                    this.Jokes.Move(index, index - 1);
                }
            });
            this.MoveDownCommand = new RelayCommand<Joke>(joke =>
            {
                if (joke == null) { return; }
                var index = this.Jokes.IndexOf(joke);
                if (index < this.Jokes.Count - 1)
                {
                    this.Jokes.Move(index, index + 1);
                }
            });
            this.UpdateCommand = new AsyncRelayCommand<Joke>(async joke =>
            {
                if (joke == null) { return; }
                var index = this.Jokes.IndexOf(joke);
                using var response = await this._httpClient.GetAsync("https://official-joke-api.appspot.com/jokes/random");
                var jokeJson = await response.Content.ReadAsStringAsync();
                var deserializeOption = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                };
                var newJoke = JsonSerializer.Deserialize<Joke>(jokeJson, deserializeOption);
                if (newJoke is null)
                {
                    throw new FormatException("Invalid json format");
                }
                this.Jokes[index] = newJoke;
            });
            this.RemoveCommand = new RelayCommand<Joke>(joke =>
            {
                if (joke is not null) this.Jokes.Remove(joke);
            });
            this.ClearCommand = new RelayCommand(() => this.Jokes.Clear());

            this.LoadCommand.Execute(null);
        }

        private bool AddJokeAvailable()
        {
            return !this._isLoading && !this.LoadCompleted;
        }

        private async Task AddJoke()
        {
            try
            {
                this._isLoading = true;
                for(int i = 0; i < 100; i++)
                if (!this.LoadCompleted && await this._jokeEnumerator.MoveNextAsync())
                {
                    this.Jokes.Add(this._jokeEnumerator.Current);
                }
                else
                {
                    this.LoadCompleted = true;
                    await this._jokeEnumerator.DisposeAsync();
                    this._httpClient.Dispose();
                }
            }
            catch (Exception ex)
            {
                this.SetError(ex.Message);
            }
            finally
            {
                this._isLoading = false;
            }
        }

        private async IAsyncEnumerator<Joke> GetJokes()
        {
            foreach (var i in Enumerable.Range(0, 50))
            {
                await Task.Delay(1000).ConfigureAwait(false);
                using var response = await this._httpClient.GetAsync("https://official-joke-api.appspot.com/jokes/random").ConfigureAwait(false);
                var jokeJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var deserializeOption = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                };
                var joke = JsonSerializer.Deserialize<Joke>(jokeJson, deserializeOption);
                if (joke is null)
                {
                    throw new FormatException("Invalid json format");
                }
                yield return joke;
            }
        }

        private async IAsyncEnumerator<Joke> Get10Jokes()
        {
            foreach (var i in Enumerable.Range(0, 50))
            {
                await Task.Delay(1000).ConfigureAwait(false);
                using var response = await this._httpClient.GetAsync("https://official-joke-api.appspot.com/random_ten").ConfigureAwait(false);
                var jokeJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var deserializeOption = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                };
                var jokes = JsonSerializer.Deserialize<IEnumerable<Joke>>(jokeJson, deserializeOption);
                if (jokes is null)
                {
                    throw new FormatException("Invalid json format");
                }
                foreach (var joke in jokes)
                {
                    yield return joke;
                }
            }
        }

        private void SetError(string errorMessage)
        {
            this.HasError = true;
            this.ErrorMessage = errorMessage;
        }
    }
}
