# 🌾 Armprod Valley

Welcome to **Armprod Valley**, a 2D farming RPG inspired by titles like *Stardew Valley*, built in the **Godot** game engine (C#). Grow crops, take care of your farm, upgrade your tools, and build your own homestead! 🚜✨

---

## 🎮 About the Game

Armprod Valley combines classic farming mechanics with smooth, modern controls. The game features:
* 🌱 **Farming:** Tilling soil (`_hoe`), watering (`_can`), planting seeds (`_seed`), harvesting with a scythe (`_scythe`), and clearing stones or plants with a pickaxe (`_pickaxe`).
* ⏱️ **Real-Time Growth System:** Crops grow in real-time based on an advanced phase and state tracking system.
* 🐝 **Placeable Objects:** Ability to build and manage beehives (`Beehive`), fruit trees (`FruitTree`), and other structures.
* 💾 **Data Saving:** A robust JSON-based system for saving game progress, inventory, player positions, and farm states.
* 💰 **Inventory & Economy Management:** Dynamic slot systems and an economy featuring earnings from your harvest.

---

## ⌨️ Controls

* **Movement:** `W`, `A`, `S`, `D` (or arrow keys) 🏃‍♂️
* **Inventory Slot Selection:** Keys `0` to `9` 🎒
* **Tool Use / Action:** Left mouse button (`action_use`) 🖱️

---

## 🛠️ Project Architecture

The project is written in **C#** for the Godot Engine and utilizes a modular structure:
* 🌾 `FarmingSystem.cs` – Handles tile map layer logic, planting, crop growth, and tool interactions.
* 💾 `SaveManager.cs` – Manages serialization and deserialization of data to JSON format (`user://saves/`).
* ⏰ `TimeManager.cs` – Tracks game time, days, and total session duration.
* 🏃 `Player.cs` – Player movement and animation logic.

---

## 🚀 Installation & Setup

1. Make sure you have **Godot Engine** installed (with .NET / C# support). 💻
2. Clone or download this repository to your computer. 📥
3. Open the Godot Project Manager, click **Import**, and select the `project.godot` file in the project directory. 📂
4. Run the project by pressing the **Play** button (F5). ▶️

---

## 📜 License

This project is developed for educational and personal purposes. 🎓
