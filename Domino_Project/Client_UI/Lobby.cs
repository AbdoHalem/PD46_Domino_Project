using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client_UI
{
    public struct RoomData
    {
        public string RoomName;
        public string MaxPlayers;
        public string CurrentPlayers;
    }

    public partial class Lobby : Form
    {
        List<RoomData> dummyRooms = new List<RoomData>();
        private List<RoomData> _allRooms = new List<RoomData>();

        // This will now act as a persistent cache for ALL created panels
        Dictionary<string, Panel> _activeCards = new Dictionary<string, Panel>();
        private Dictionary<string, Action<string[]>> _serverCommandHandlers;

        private int _currentPage = 0;
        private const int PageSize = 6;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, bool wParam, int lParam);
        private const int WM_SETREDRAW = 11;

        public Lobby()
        {
            InitializeComponent();

            typeof(FlowLayoutPanel).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic,
            null,
            flowLayoutPanel1,
            new object[] { true });

            _serverCommandHandlers = new Dictionary<string, Action<string[]>>(StringComparer.OrdinalIgnoreCase)
            {
                { "ALLROOMS", HandleAllRooms },
                { "NEWROOM", HandleNewRoom }, // Added handler for individual new rooms
                { "UPDATE", HandleFrequentUpdateCommand } // Map standard string[] array to our targeted update method
            };

            button1.Click += ButtonNext_Click;
            button2.Click += ButtonPrev_Click;
            UpdatePaginationButtons();
        }

        public void LoadRooms(List<RoomData> rooms)
        {
            _allRooms = rooms ?? new List<RoomData>();
            _currentPage = 0;
            RenderCurrentPage();
        }

        private void RenderCurrentPage()
        {
            // 1. Tell Windows to completely stop drawing the panel
            SendMessage(flowLayoutPanel1.Handle, WM_SETREDRAW, false, 0);

            flowLayoutPanel1.SuspendLayout();

            // 2. Remove controls from view but DO NOT dispose them to preserve the cache
            flowLayoutPanel1.Controls.Clear();

            var pageItems = _allRooms.Skip(_currentPage * PageSize).Take(PageSize);

            var cardsToAdd = new List<Control>();

            foreach (var room in pageItems)
            {
                // 3. Check if the card is already in the dictionary
                if (!_activeCards.TryGetValue(room.RoomName, out Panel currCard))
                {
                    // 4. If it doesn't exist, create it and save it to the dictionary
                    currCard = CreateRoomCard(room);
                    _activeCards[room.RoomName] = currCard;
                }

                // 5. Add the cached (or newly created) card to the temporary list
                cardsToAdd.Add(currCard);
            }

            // 6. Bulk add the array of controls to the layout panel
            flowLayoutPanel1.Controls.AddRange(cardsToAdd.ToArray());

            flowLayoutPanel1.ResumeLayout();

            // 7. Tell Windows it is allowed to draw again
            SendMessage(flowLayoutPanel1.Handle, WM_SETREDRAW, true, 0);

            // 8. Force a single, clean repaint of the panel with the new controls
            flowLayoutPanel1.Refresh();

            UpdatePaginationButtons();
        }

        // --- NEW: Function to handle a single newly created room ---
        private void HandleNewRoom(string[] data)
        {
            // Expected format from server: NEWROOM|RoomName,MaxPlayers,CurrentPlayers
            if (data.Length == 0) return;

            string[] parts = data[0].Split(',');
            if (parts.Length >= 3)
            {
                RoomData newRoom = new RoomData
                {
                    RoomName = parts[0],
                    MaxPlayers = parts[1],
                    CurrentPlayers = parts[2]
                };

                // Add to list if it doesn't already exist
                if (!_allRooms.Any(r => r.RoomName == newRoom.RoomName))
                {
                    _allRooms.Add(newRoom);

                    // Re-render if the current page isn't full yet so the user sees it immediately
                    int currentDisplayedCount = flowLayoutPanel1.Controls.Count;
                    if (currentDisplayedCount < PageSize && _allRooms.Count > _currentPage * PageSize)
                    {
                        RenderCurrentPage();
                    }
                    else
                    {
                        // Otherwise, just update the buttons so they can navigate to it
                        UpdatePaginationButtons();
                    }
                }
            }
        }

        private void ButtonNext_Click(object sender, EventArgs e)
        {
            if ((_currentPage + 1) * PageSize < _allRooms.Count)
            {
                _currentPage++;
                RenderCurrentPage();
            }
        }

        private void ButtonPrev_Click(object sender, EventArgs e)
        {
            if (_currentPage > 0)
            {
                _currentPage--;
                RenderCurrentPage();
            }
        }

        private void UpdatePaginationButtons()
        {
            button1.Enabled = (_currentPage + 1) * PageSize < _allRooms.Count;
            button2.Enabled = _currentPage > 0;
        }

        public Panel CreateRoomCard(RoomData room)
        {
            Panel cardPanel = new Panel
            {
                Name = $"RoomCard_{room.RoomName}",
                Size = new Size(210, 275),
                Margin = new Padding(8),
                BackgroundImage = Properties.Resources.button_background2,
                BackgroundImageLayout = ImageLayout.Stretch,
                RightToLeft = RightToLeft.Yes
            };

            cardPanel.Controls.Add(new Label
            {
                Text = $"غرفة: {room.RoomName}",
                AutoSize = true,
                Font = new Font("Tahoma", 12, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(65, 10)
            });

            Label lblCount = new Label
            {
                Name = "lblPlayerCount",
                Tag = room.MaxPlayers,
                Text = $"اللاعبين: {room.CurrentPlayers}/{room.MaxPlayers}",
                AutoSize = true,
                Font = new Font("Tahoma", 10, FontStyle.Regular),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(120, 65)
            };
            cardPanel.Controls.Add(lblCount);

            bool isFull = room.CurrentPlayers == room.MaxPlayers;

            cardPanel.Controls.Add(new Button
            {
                Name = "btnJoin",
                Text = "انضمام",
                Size = new Size(80, 35),
                Location = new Point(10, 220),
                Enabled = !isFull
            });

            cardPanel.Controls.Add(new Button
            {
                Name = "btnWatch",
                Text = "مشاهدة",
                Size = new Size(80, 35),
                Location = new Point(120, 220),
                Enabled = isFull
            });

            return cardPanel;
        }

        public void ToggleCardButtons(Panel targetCard)
        {
            if (targetCard == null) return;

            Button btnJoin = targetCard.Controls.Find("btnJoin", false).FirstOrDefault() as Button;
            Button btnWatch = targetCard.Controls.Find("btnWatch", false).FirstOrDefault() as Button;

            if (btnJoin != null && btnWatch != null)
            {
                btnJoin.Enabled = !btnJoin.Enabled;
                btnWatch.Enabled = !btnWatch.Enabled;
            }
        }

        public void UpdatePlayerCountText(Panel targetCard, string newCurrentPlayers)
        {
            if (targetCard == null) return;

            Label lblPlayerCount = targetCard.Controls.Find("lblPlayerCount", false).FirstOrDefault() as Label;
            Button btnJoin = targetCard.Controls.Find("btnJoin", false).FirstOrDefault() as Button;
            Button btnWatch = targetCard.Controls.Find("btnWatch", false).FirstOrDefault() as Button;

            if (lblPlayerCount != null)
            {
                string maxPlayers = lblPlayerCount.Tag?.ToString() ?? "0";

                lblPlayerCount.Text = $"اللاعبين: {newCurrentPlayers}/{maxPlayers}";

                if (btnJoin != null && btnWatch != null)
                {
                    int current = 0;
                    int max = 0;

                    int.TryParse(newCurrentPlayers, out current);
                    int.TryParse(maxPlayers, out max);

                    if (current >= max && max > 0)
                    {
                        btnJoin.Enabled = false;
                        btnWatch.Enabled = true;
                    }
                    else
                    {
                        btnJoin.Enabled = true;
                        btnWatch.Enabled = false;
                    }
                }
            }
        }

        public void ProcessIncomingData(string rawData)
        {
            Task.Run(() =>
            {
                string[] tokens = rawData.Split('|');
                if (tokens.Length == 0) return;

                string action = tokens[0];
                string[] data = tokens.Skip(1).ToArray();

                this.Invoke(new Action(() =>
                {
                    if (_serverCommandHandlers.TryGetValue(action, out var handler))
                    {
                        handler(data);
                    }
                }));
            });
        }

        private void HandleAllRooms(string[] roomTokens)
        {
            var parsedRooms = roomTokens
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Split(','))
                .Where(parts => parts.Length >= 3)
                .Select(parts => new RoomData
                {
                    RoomName = parts[0],
                    MaxPlayers = parts[1],
                    CurrentPlayers = parts[2]
                }).ToList();

            if (this.InvokeRequired)
                this.Invoke(new Action(() => LoadRooms(parsedRooms)));
            else
                LoadRooms(parsedRooms);
        }

        // Helper to unwrap string array for the action dictionary
        private void HandleFrequentUpdateCommand(string[] data)
        {
            if (data.Length >= 2)
            {
                // Expected command format: UPDATE|RoomName|NewCount
                HandleFrequentUpdate(data[0], data[1]);
            }
        }

        public void HandleFrequentUpdate(string roomName, string newCount)
        {
            // 1. Update the master data list
            int index = _allRooms.FindIndex(r => r.RoomName == roomName);
            if (index != -1)
            {
                var room = _allRooms[index];
                room.CurrentPlayers = newCount;
                _allRooms[index] = room;
            }

            // 2. Check the Cache Dictionary. 
            // If it hasn't been created yet, this safely ignores it.
            // If it HAS been created, it updates the visual element immediately (even if the card is off-page).
            if (_activeCards.TryGetValue(roomName, out Panel cachedCard))
            {
                UpdatePlayerCountText(cachedCard, newCount);
            }
        }

        private async void TestPlayerCountUpdateAsync()
        {
            await Task.Delay(5000);
            HandleFrequentUpdate("المررحفين", "4");

            // Testing the NEWROOM functionality
            await Task.Delay(2000);
            ProcessIncomingData("NEWROOM|غرفة_جديدة,3,1");
        }

        private void Lobby_Load(object sender, EventArgs e)
        {
            string incomingString = "ALLROOMS|المررحفين,4,3|المحؤؤترفين,4,3|المحتققرفن,4,3|المحلترفين,4,3المحبفين,4,3|امحفين,4,3|لمحفين,4,3|المحفي,4,3|المحفن,4,3|الحفين,4,3|المحين,4,3|المحفين,4,3|";
            ProcessIncomingData(incomingString);

            TestPlayerCountUpdateAsync();
        }
    }
}