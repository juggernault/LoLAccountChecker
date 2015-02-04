#region

using System.Collections.Generic;
using System.Linq;
using PVPNetConnect;
using PVPNetConnect.RiotObjects.Platform.Catalog.Champion;

#endregion

namespace LoL_Account_Checker
{
    public delegate void ReceivedData(object sender, AccountData data);

    internal class Client
    {
        public AccountData Data;
        private PVPNetConnection _connection;
        private bool _isDone;

        public Client(Region region, string username, string password)
        {
            _isDone = false;
            Data = new AccountData { Username = username, Password = password };

            _connection = new PVPNetConnection();
            _connection.OnLogin += OnLogin;

            _connection.Connect(username, password, region, "5.2.15");
        }

        public bool IsDone
        {
            get { return _isDone; }
        }

        public event ReceivedData OnReceivedData;

        public void Disconnect()
        {
            _connection.Disconnect();
        }

        private void OnLogin(object sender, string username, string ipAddress)
        {
            GetData();
        }

        private async void GetData()
        {
            var loginPacket = await _connection.GetLoginDataPacketForUser();
            if (loginPacket.AllSummonerData == null)
            {
                return;
            }

            var champions = await _connection.GetAvailableChampions();
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

            ReceivedData(Data);
        }

        protected virtual void ReceivedData(AccountData data)
        {
            if (OnReceivedData != null)
            {
                OnReceivedData(this, data);
            }
        }
    }
}