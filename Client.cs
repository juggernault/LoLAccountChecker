#region

using System;
using System.Collections.Generic;
using System.Linq;
using PVPNetConnect;
using PVPNetConnect.RiotObjects.Platform.Catalog.Champion;

#endregion

namespace LoL_Account_Checker
{
    public delegate void Report(object sender, Client.Result result);

    public class Client
    {
        public enum Result
        {
            Success,
            Error
        }

        private bool _completed;
        public bool Completed { get { return _completed; } }

        public PVPNetConnection Connection;
        public AccountData Data;
        public string ErrorMessage;

        public Client(Region region, string username, string password)
        {
            Data = new AccountData { Username = username, Password = password };
            _completed = false;

            Connection = new PVPNetConnection();
            Connection.OnLogin += OnLogin;
            Connection.OnError += OnError;

            Connection.Connect(username, password, region, "5.2.15");

            Console.WriteLine("[{0:HH:mm}] <{1}> Connecting to PvP.net", DateTime.Now, Data.Username);
        }

        public event Report OnReport;

        public void Disconnect()
        {
            Connection.Disconnect();
            Console.WriteLine("[{0:HH:mm}] <{1}> Disconnecting", DateTime.Now, Data.Username);
        }

        private void OnLogin(object sender, string username, string ipAddress)
        {
            Console.WriteLine("[{0:HH:mm}] <{1}> Logged in!", DateTime.Now, Data.Username);
            GetData();
        }

        private void OnError(object sender, Error error)
        {
            Console.WriteLine("[{0:HH:mm}] <{1}> Error: {2}", DateTime.Now, Data.Username, error.Message);
            ErrorMessage = error.Message;
            _completed = true;
            Report(Result.Error);
        }

        private async void GetData()
        {
            var loginPacket = await Connection.GetLoginDataPacketForUser();
            if (loginPacket.AllSummonerData == null)
            {
                return;
            }

            var champions = await Connection.GetAvailableChampions();
            var skins = new List<ChampionSkinDTO>();

            foreach (var champion in champions)
            {
                skins.AddRange(champion.ChampionSkins.Where(s => s.Owned));
            }


            Data.Level = (int) loginPacket.AllSummonerData.SummonerLevel.Level;
            Data.RpBalance = (int) loginPacket.RpBalance;
            Data.Ipbalance = (int) loginPacket.IpBalance;
            Data.Champions = champions.Count(c => c.Owned);
            Data.Skins = skins.Count;
            Data.RunePages = loginPacket.AllSummonerData.SpellBook.BookPages.Count;
            Data.SummonerName = loginPacket.AllSummonerData.Summoner.Name;

            Console.WriteLine("[{0:HH:mm}] <{1}> Data received!", DateTime.Now, Data.Username);
            _completed = true;
            Report(Result.Success);
        }

        protected virtual void Report(Result result)
        {
            if (OnReport != null)
            {
                OnReport(this, result);
            }
        }
    }
}