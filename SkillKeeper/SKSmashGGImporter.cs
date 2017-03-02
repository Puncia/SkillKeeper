﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SmashGGApiWrapper;

namespace SkillKeeper
{
    public partial class SKSmashGGImporter : Form
    {
        private SmashGGPortal portal;
        private List<Tournament> tournaments = new List<Tournament>();
        private List<Match> importMatches = new List<Match>();
        private List<Person> importPlayers = new List<Person>();
        private List<Person> currentPlayers = new List<Person>();
        private List<String> playerNames = new List<String>();
        private List<ImportPlayer> challongePlayerList = new List<ImportPlayer>();
        private List<String> eventList = new List<String>();
        private String tournamentName = "";

        private List<String> addAlts = new List<String>();

        private Tournament curTourney = new Tournament();

        private Tournament specifiedTournament;

        public int phaseID { get; set; }
        public int eventID { get; set; }
        public int groupID { get; set; }
        public bool importCompleteBracket { get; set; }

        public SKSmashGGImporter()
        {
            InitializeComponent();
            sKLinkDataGridViewTextBoxColumn.DataSource = playerNames;
            playerNames.Add(" << Create New Player >>");
            importButton.DialogResult = DialogResult.OK;
        }
        public void importSmashGGBracket(string tournament, List<Person> playerList )
        {
            portal = new SmashGGPortal(tournament);
            //Load current players in leaderboard
            foreach (Person p in playerList)
            {
                currentPlayers.Add(p);
                playerNames.Add(p.Name);
            }
            playerNames.Sort();
            IEnumerable<Group> groups = new List<Group>();
            if (importCompleteBracket)
            {
                groups = portal.GetGroups(eventID);
                foreach (Group g in groups)
                {
                    createPlayer(g.id);
                }
            }
            else
            {
                createPlayer(groupID);
            }
        }
        private void createPlayer(int gID)
        {
            foreach (Entrant e in portal.GetEntrants(gID))
            {
                ImportPlayer ip = new ImportPlayer();
                ip.ID = e.id.ToString();
                ip.Name = e.name;
                ip.SKLink = getMatch(e.name);
                challongePlayerList.Add(ip);
            }
            importPlayerBindingSource.DataSource = new BindingList<ImportPlayer>(challongePlayerList);
        }

        private void importPlayerList_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            foreach(ImportPlayer p in challongePlayerList)
            {
                if(p.ID == (string)importPlayerList.CurrentRow.Cells[0].Value)
                {
                    p.SKLink = (string)importPlayerList.CurrentRow.Cells[2].Value;
                }
            }
        }
        private string getMatch(string playerName)
        {
            string result = " << Create New Player >>";
            bool foundMatch = false;
            foreach (Person person in currentPlayers)
            {
                if(person.Name.ToUpper() == playerName.ToUpper() || (playerName.ToUpper().StartsWith(person.Team.ToUpper()) && playerName.ToUpper().EndsWith(person.Name.ToUpper()) && person.Name.Length > 0 && person.Team.Length > 0))
                {
                    foundMatch = true;
                    result = person.Name;
                    break;
                }
            }
            if (!foundMatch)
            {
                foreach (Person person in currentPlayers)
                {
                    foreach (string altName in person.Alts)
                    {
                        if (altName.ToUpper() == playerName.ToUpper() || (playerName.ToUpper().StartsWith(person.Team.ToUpper()) && playerName.ToUpper().EndsWith(altName.ToUpper()) && altName.Length > 0 && person.Team.Length > 0))
                        {
                            result = person.Name;
                        }
                    }
                }
            }
            return result;
        }

        private void importButton_Click(object sender, EventArgs e)
        {
            foreach (ImportPlayer p in challongePlayerList)
            {
                if (p.SKLink == " << Create New Player >>")
                {
                    if (!playerNames.Contains(p.Name))
                    {
                        Person person = new Person();
                        person.Name = p.Name;
                        importPlayers.Add(person);
                    }
                }
                else if (p.SKLink != p.Name)
                {
                    addAlts.Add(p.SKLink + "\t" + p.Name);
                }
            }
            if(importCompleteBracket)
            {
                foreach(Group g in portal.GetGroups(eventID))
                {
                    importSets(g.id);
                }
            }
            else
            {
                importSets(groupID);
            }

            this.Close();
        }
        private void importSets(int gID)
        {

            foreach (Set s in portal.GetMatches(gID))
            {
                if (s.completedAt != null)
                {
                    createMatch(s.entrant1Id, s.entrant2Id, s.winnerId);
                }
            }

        }
        //TODO: Fix for Smash gg
        private void createMatch(int? p1ID, int? p2ID, int? WinnerID)
        {
            Match m = new Match();

            //m.Description = curTourney.FullChallongeUrl.Split('.', '/').Skip(2).FirstOrDefault() + " - " + eventSelector.Text;
            m.Timestamp = DateTime.Now;

            foreach (ImportPlayer p in challongePlayerList)
            {
                if (p.ID == p1ID.ToString())
                {
                    if (p.SKLink == " << Create New Player >>")
                        m.Player1 = p.Name;
                    else
                        m.Player1 = p.SKLink;
                }
                if (p.ID == p2ID.ToString())
                {
                    if (p.SKLink == " << Create New Player >>")
                        m.Player2 = p.Name;
                    else
                        m.Player2 = p.SKLink;
                }
            }

            if (m.Player1 != null && m.Player2 != null)
            {
                if (p1ID == WinnerID)
                    m.Winner = 1;
                else if (p2ID == WinnerID)
                    m.Winner = 2;
                else
                    m.Winner = 0;

                m.ID = Guid.NewGuid().ToString("N");

                importMatches.Add(m);
            }
        }
        public List<Person> getImportPlayers()
        {
            return importPlayers;
        }

        public List<Match> getImportMatches()
        {
            return importMatches;
        }

        public List<String> getNewAlts()
        {
            return addAlts;
        }
    }
}
