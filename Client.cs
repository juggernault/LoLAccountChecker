#region

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

        public PVPNetConnection Connection;
        public AccountData Data;

        public Client(Region region, string username, string password)
        {
            Data = new AccountData { Username = username, Password = password };

            Connection = new PVPNetConnection();
            Connection.OnLogin += OnLogin;

            Connection.OnError += OnError;

            Connection.Connect(username, password, region, "5.2.15");
        }

        public event Report OnReport;

        public void Disconnect()
        {
            Connection.Disconnect();
        }

        private void OnLogin(object sender, string username, string ipAddress)
        {
            GetData();
        }

        private void OnError(object sender, Error error)
        {
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