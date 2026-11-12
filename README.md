# BoardGame

A multi-game board game engine written in C# / .NET 8, featuring terminal TUI demos and a mobile-friendly local web app with real-time multiplayer.

## Games Included
- 🔴 **Connect Four**: 7×6 gravity grid with 4-in-a-row win detection.
- ❌ **Tic-Tac-Toe**: 3×3 grid featuring minimax solver AI.
- 🍫 **Chomp**: 5×4 chocolate bar game-theory puzzle with memoized solver. Top-left cell `(0, 0)` is poison!

---

## 📱 Mobile-Friendly Local Web App

Play on your phone, tablet, or desktop over your local Wi-Fi network without installing any apps!

### Starting the Web Server
```bash
./play-web.sh
```
*Note: Auto-recompilation (watch mode) is enabled by default. Any saved changes to C# or web files reload automatically. Use `--no-watch` for standard execution.*

Or specify a custom port:
```bash
./play-web.sh --port 8080
```

### Playing from Your Mobile Device
1. Ensure your phone is connected to the same Wi-Fi network as your computer.
2. Open your mobile browser and navigate to the LAN URL displayed when `./play-web.sh` starts (e.g., `http://192.168.1.X:5000`).
3. **Scan to Join**: Tap the **Share** button in the web app to view an on-screen QR code for friends and family to scan and join instantly.

### Supported Play Modes
- 📱 **Pass & Play (Hotseat)**: Take turns on a single phone or tablet.
- 🤖 **vs AI**: Play solo against Casual or Smart AI opponents (unbeatable Minimax in Tic-Tac-Toe, memoized solver in Chomp).
- 🌐 **Wi-Fi Room**: Host a room on your computer or phone; another phone joins with a 4-letter room code for real-time multiplayer over Server-Sent Events (SSE).

> **Firewall Note (Linux / Bazzite / Fedora)**:
> If devices on your local network cannot connect, ensure port 5000 is open in your local firewall:
> ```bash
> sudo firewall-cmd --add-port=5000/tcp --temporary
> ```

---

## ⚙️ Running as a Systemd Background Service

You can run the web server as a systemd user unit (no `sudo` required):

### Install & Enable
```bash
# 1. Symlink unit to ~/.config/systemd/user/ and publish Release binary
./install-service.sh

# 2. Enable & start immediately
systemctl --user enable --now boardgame-web.service
```

### Manage the Service
```bash
# Check status
systemctl --user status boardgame-web.service

# View live journal logs
journalctl --user -u boardgame-web.service -f

# Restart or stop
systemctl --user restart boardgame-web.service
systemctl --user stop boardgame-web.service

# Uninstall
./install-service.sh --uninstall
```

---

## 🖥️ Terminal TUI Automated Demo

Run the animated in-place terminal demo:
```bash
./play-demo.sh [connect4|tictactoe|chomp|all] [--delay <ms>] [--alt|--no-alt]
```

---

## 🧪 Testing

Run the full unit test suite (208 tests):
```bash
dotnet test BoardGame/BoardGame.Tests/BoardGame.Tests.csproj --no-restore
```
