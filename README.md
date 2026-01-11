# 🎵 PALT Factory – Music App

Welcome to **PALT Factory Music App**, a desktop application for managing music libraries and playlists.

The application is built using modern Microsoft technologies and follows a database-first approach.

---

## 🛠 Tech Stack

- **Language:** C# 10  
- **Framework:** .NET 8  
- **UI:** WPF  
- **ORM:** Entity Framework Core  
- **Database:** SQL Server (Database First)

---

## 🚀 Getting Started

To be able to run the application locally, follow these steps:

### 1. Clone the Repository
- Clone the project from GitHub
- Open the solution in **Visual Studio 2022** (or later)

### 2. Configure User Secrets
The application uses **User Secrets** to store sensitive information such as the database connection string.

1. Right-click on the project **`MusicLibrary`**
2. Select **Manage User Secrets**
3. Paste your `ConnectionString` into the `secrets.json` file.

---

## 🎧 Use the App

### Start / Home View

In the **Start/Home view**, the application is divided into multiple sections:

- On the **left side**, you can select a playlist.
- The **right view** shows the tracks in the selected playlist.
- You can delete tracks from a playlist by clicking on the 🗑 bin icon.
- In the **middle column**, you can browse **Artists**, **Albums**, and **Tracks**.
- By double-clicking a track, it will be added to the selected playlist.

---

### ✏️ Edit Menu

In the **Edit menu**, you can:

- Add / Update / Delete **Playlists**
- Add / Update / Delete **Artists**
- Add / Update / Delete **Albums**
- Add / Update / Delete **Tracks**

All changes are saved directly in the application.

---

## ✅ Summary

- Manage playlists and music content
- Add tracks to playlists using double-click
- Edit all entities through the Edit menu

---

Enjoy using the PALT Music Library App! 🎶
