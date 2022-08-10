using Grpc.Core;
using Billing.Model;
using System.Text.Json;
using Newtonsoft.Json;
using Billing.Helpers.Interfaces;

namespace Billing.Services
{
    public class BillingService : Billing.BillingBase
    {
        private readonly IResponse _response;
        private readonly ILogger<BillingService> _logger;
        private readonly List<Person> _people;
        private Dictionary<Coin, Person> _coins;

        public BillingService(IResponse response, ILogger<BillingService> logger)
        {
            _response = response;
            _logger = logger;
            _coins = new Dictionary<Coin, Person>();

            using StreamReader sr = new StreamReader("user.json");
            _people = JsonConvert.DeserializeObject<People>(sr.ReadToEnd()).Person;
            foreach (Person person in _people)
            {
                person.Profile = new UserProfile() { Name = person.Name, Amount = 0 };
            }
        }

        public override async Task ListUsers(None request, IServerStreamWriter<UserProfile> responseStream, ServerCallContext context)
        {            
            foreach (var user in _people)
            {
                await responseStream.WriteAsync(user.Profile);
            }
            _logger.LogInformation("list users query");
        }

        public override async Task<Response> CoinsEmission(EmissionAmount request, ServerCallContext context)
        {
            if (request.Amount < _people.Count)
            {
                return _response.ResponseFail("Quantity of coins is less than users.");
            }
            await Task.Run(() => DistributeCoinsUsers(request.Amount));
            return _response.ResponseOk("Coins distributed successfully.");
        }

        public override async Task<Response> MoveCoins(MoveCoinsTransaction request, ServerCallContext context)
        {
            Person srsUser = _people.Where(p => p.Name == request.SrcUser).FirstOrDefault();
            Person dstUser = _people.Where(p => p.Name == request.DstUser).FirstOrDefault();
            int amountCoins = (int)request.Amount;

            if (srsUser is null)
            {
                return _response.ResponseFail($"user named {request.SrcUser} not found");
            }

            if (dstUser is null)
            {
                return _response.ResponseFail($"user named {request.DstUser} not found");
            }

            if (amountCoins > srsUser.Profile.Amount)
            {
                return _response.ResponseFail($"user named {request.SrcUser} doesn't have enough coins");
            }

            await Task.Run(() => MoveCoinsUsers(amountCoins, srsUser, dstUser));

            return _response.ResponseOk($"User named {srsUser.Name} transferred {amountCoins} coins to a user named {dstUser.Name}");
        }

        public override async Task<Coin> LongestHistoryCoin(None request, ServerCallContext context)
        {
            Coin coin = new Coin();
            int maxLength = default;
            foreach (KeyValuePair<Coin, Person> item in _coins)
            {
                int count = item.Key.History.Split('\n').Length;
                if (count > maxLength)
                {
                    coin = item.Key;
                    maxLength = count;
                }
            }
            _logger.LogInformation("Longest history coin query");
            return coin;
        }

        private void MoveCoinsUsers(int amountCoins, Person srsUser, Person dstUser)
        {
            List<KeyValuePair<Coin, Person>> coins = _coins.Where(c => c.Value == srsUser).Take(amountCoins).ToList();
            foreach (KeyValuePair<Coin, Person> coin in coins)
            {
                srsUser.Profile.Amount--;
                dstUser.Profile.Amount++;
                _coins[coin.Key] = dstUser;
                coin.Key.History += GetRepresentationHistory(srsUser.Name, dstUser.Name);
                _logger.LogInformation($"Coin id: {coin.Key.Id} new owner: {dstUser.Name}");
            }
        }

        private void DistributeCoinsUsers(decimal inputAmountCoins)
        {
            decimal usersProcessed = default, coinsDist = default, amountCoins = default, quantityCoins = default;
            decimal coefficient = inputAmountCoins / _people.Sum(x => x.Rating);
            List<Person> peopleDist = new List<Person>();
            foreach (Person person in _people)
            {
                usersProcessed += person.Rating;
                amountCoins = Math.Round(usersProcessed * coefficient - coinsDist);
                if (amountCoins < 1)
                {
                    amountCoins = 1;
                    CreateCoin((int)amountCoins, person);
                    quantityCoins++;
                }
                else
                {
                    peopleDist.Add(person);
                }
                coinsDist += amountCoins;
            }

            coefficient = (inputAmountCoins - quantityCoins) / peopleDist.Sum(x => x.Rating);
            usersProcessed = default;
            coinsDist = default;
            foreach (Person person in peopleDist)
            {
                usersProcessed += person.Rating;
                amountCoins = Math.Round(usersProcessed * coefficient - coinsDist);
                coinsDist += amountCoins;
                CreateCoin((int)amountCoins, person);
            }
        }

        private void CreateCoin(int amountCoins, Person user)
        {
            user.Profile.Amount += amountCoins;
            for (int i = 0; i < amountCoins; i++)
            {
                Coin coin = new() { Id = BitConverter.ToInt64(Guid.NewGuid().ToByteArray()), History = GetRepresentationHistory("Emission", user.Name) };
                _coins.Add(coin, user);
                _logger.LogInformation($"Create coin with id {coin.Id} user: {user.Name}");
            }
        }

        private string GetRepresentationHistory(string from, string to)
        {
            return $"{from} ==> {to}\n";
        }
    }
}
