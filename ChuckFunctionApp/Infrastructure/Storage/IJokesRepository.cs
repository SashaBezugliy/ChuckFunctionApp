using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChuckFunctionApp.Infrastructure.Storage
{
    public interface IJokesRepository
    {
        Task InitializeAsync();
        Task<int> InsertJokeAsync(Joke joke);
    }
}
