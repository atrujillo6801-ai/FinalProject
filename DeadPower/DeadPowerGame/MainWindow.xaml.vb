Imports Microsoft.VisualBasic
Imports System.IO
Imports Newtonsoft.Json



Class MainWindow
    '--------------------------------------------------------------------------------------------------------------
    'Declarations for Variables that are used across the entire code 
    '--------------------------------------------------------------------------------------------------------------
    Enum MoveKey
        Left = 0
        Right = 1
        Up = 2
        Down = 3
    End Enum

    Dim currentRoom As Room
    Dim gameRooms As Dictionary(Of String, Room)
    Dim player As Player
    Dim lightsOn As Boolean = False
    Dim leftKey As Boolean
    Dim rightKey As Boolean
    Dim upKey As Boolean
    Dim downKey As Boolean
    Dim speed As Integer

    Private rectPlayer As Rect
    Private rectNorth As Rect
    Private rectSouth As Rect
    Private rectWest As Rect
    Private rectEast As Rect


    Dim basePath As String = System.IO.Directory.GetCurrentDirectory()
    Dim relativePath As String = ""
    Dim fullPath As String = Path.Combine(basePath, relativePath)


    Dim flashlight As New Item With {
        .Name = "Flashlight",
        .Description = "A handy flashlight to illuminate dark rooms.",
        .HealAmount = 0,
        .AttackBonus = 0,
        .ItemType = "Tool"
    }

    Dim radio As New Item With {
        .Name = "Emergency Radio",
        .Description = "A radio that can call for help if you can find a way to power it.",
        .HealAmount = 0,
        .AttackBonus = 0,
        .ItemType = "Tool"
    }






    '--------------------------------------------------------------------------------------------------------------
    'Main Game Logic
    '--------------------------------------------------------------------------------------------------------------
    Public Sub New()
        InitializeComponent()

        ' Create dictionary of rooms
        gameRooms = New Dictionary(Of String, Room)


        ' Create player
        player = New Player("Sam Stones")
        lblPlayerName.Content = player.Name

        'we are declaring that a room will be created from the blueprint class of room, and we will call it "entrance". 
        'We will then set the properties of this room (name, description, etc.from the blueprint properties) to create a unique location in our game world.]
        Dim entrance As New Room()
        entrance.Name = "West Entrance Hall"
        entrance.Description = "Looks like a deserted building. I wonder if I can find a radio inside? (Use your keyboard keys to explore rooms)."
        entrance.Exits.Add("East", "East Dark Room")
        entrance.Item = flashlight

        Dim eastRoom As New Room()
        eastRoom.Name = "East Dark Room"
        eastRoom.Description = "Looks like the power is down. Maybe there is a breaker or an auxilary generator somewhere."
        eastRoom.Exits.Add("West", "West Entrance Hall")
        eastRoom.Exits.Add("North", "North Zombie Room")
        eastRoom.Exits.Add("South", "South Key Room")


        Dim northRoom As New Room()
        northRoom.Name = "North Zombie Room"
        northRoom.Description = "You find an unexpected guest."
        northRoom.Exits.Add("South", "East Dark Room")
        northRoom.Enemy = New Enemy("Zombie", 100, 10)


        Dim southRoom As New Room()
        southRoom.Name = "South Key Room"
        southRoom.Description = "A glint catches your eye from the corner of the room."
        southRoom.Exits.Add("North", "East Dark Room")





        'this adds the rooms we created to the dictionary of rooms, using the string of the room's name as the key. This allows us to easily look up any room by its name later on (like when we want to move to a new room).
        gameRooms.Add(entrance.Name, entrance)
        gameRooms.Add(northRoom.Name, northRoom)
        gameRooms.Add(eastRoom.Name, eastRoom)
        gameRooms.Add(southRoom.Name, southRoom)



        ' Set starting room
        currentRoom = entrance

        rectPlayer = imageToRect(imgPlayer)
        rectNorth = imageToRect(imgNorth)
        rectEast = imageToRect(imgEast)
        rectSouth = imageToRect(imgSouth)
        rectWest = imageToRect(imgWest)

        ' Update the screen
        UpdateRoomDisplay()
        UpdateHealthBars()
        UpdateInventoryDisplay()
        AddToLog("My plane was shot down. Maybe this building will provide the means to my escape.")

        Me.Focus() ' Set focus to the window to ensure it receives keyboard input

        AddHandler CompositionTarget.Rendering, AddressOf GameLoop


    End Sub
    Private Function imageToRect(sprite As Image) As Rect
        Return New Rect(Canvas.GetLeft(sprite), Canvas.GetTop(sprite), sprite.Width, sprite.Height)
    End Function
    Private Sub GameLoop()

        If leftKey Then CheckKeyToMove(MoveKey.Left)
        If rightKey Then CheckKeyToMove(MoveKey.Right)
        If upKey Then CheckKeyToMove(MoveKey.Up)
        If downKey Then CheckKeyToMove(MoveKey.Down)

        If CollisionTestWalls(rectPlayer, rectNorth) Then
            btnNorth_Click(Nothing, Nothing)
            AddToLog("You bumped into a wall to the north.")
        ElseIf CollisionTestWalls(rectPlayer, rectEast) Then
            btnEast_Click(Nothing, Nothing)
            AddToLog("You bumped into a wall to the east.")
        ElseIf CollisionTestWalls(rectPlayer, rectSouth) Then
            btnSouth_Click(Nothing, Nothing)
            AddToLog("You bumped into a wall to the south.")
        ElseIf CollisionTestWalls(rectPlayer, rectWest) Then
            btnWest_Click(Nothing, Nothing)
            AddToLog("You bumped into a wall to the West")
        End If

    End Sub

    Private Sub CheckKeyToMove(isKeyMove As MoveKey)
        Select Case isKeyMove
            Case isKeyMove.Left
                MoveLeft()
            Case isKeyMove.Right
                MoveRight()
            Case isKeyMove.Up
                MoveUp()
            Case isKeyMove.Down
                MoveDown()
            Case Else


        End Select
    End Sub





    'Room Logic
    Private Function CollisionTestWalls(objA As Rect, wallObj As Rect) As Boolean
        Return objA.IntersectsWith(wallObj)
    End Function
    'Loading a game
    Private Sub LoadGame()
        Dim savePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\save.json")

        If Not File.Exists(savePath) Then
            AddToLog("No save file found. Starting new game.")
            Return
        End If

        Dim json As String = File.ReadAllText(savePath)
        Dim saveData As SaveData = JsonConvert.DeserializeObject(Of SaveData)(json)

        ' Restore the player from saved data
        player = New Player(saveData.Name)
        player.Health = saveData.Health
        player.MaxHealth = saveData.MaxHealth
        player.AttackPower = saveData.AttackPower
        player.Gold = saveData.Gold
        player.Inventory = saveData.Inventory

        ' Restore the room
        currentRoom = gameRooms(saveData.CurrentRoom)

        UpdateRoomDisplay()
        UpdateHealthBars()
        UpdateInventoryDisplay()
        AddToLog("Save loaded. Welcome back, " & player.Name & "!")
    End Sub

    'Saving a game
    Private Sub SaveGame()
        Dim saveData As New SaveData ' a simple data-transfer class (see below)
        saveData.Name = player.Name
        saveData.Health = player.Health
        saveData.MaxHealth = player.MaxHealth
        saveData.AttackPower = player.AttackPower
        saveData.Gold = player.Gold
        saveData.CurrentRoom = currentRoom.Name
        saveData.Inventory = player.Inventory

        Dim json As String = JsonConvert.SerializeObject(saveData, Formatting.Indented)
        Dim savePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\save.json")
        File.WriteAllText(savePath, json)
        AddToLog("Game saved successfully.")
    End Sub

    'GameOver
    Sub ShowGameOver() ' Had to declare from scratch. Used suggestion from VS
        MessageBox.Show("Game Over! Thanks for playing.")
        Application.Current.Shutdown()
    End Sub

    'updating functions defined here
    Private Sub UpdateRoomDisplay()
        lblRoomName.Content = currentRoom.Name


        ' Change background based on lights
        If lightsOn Then
            relativePath = "Assets\images\RoomLight.png"
            mainCanvas.Background = New ImageBrush(New BitmapImage(New Uri(basePath + "/" + relativePath)))
        Else
            relativePath = "Assets\images\RoomDark.png"
            mainCanvas.Background = New ImageBrush(New BitmapImage(New Uri(basePath + "/" + relativePath)))
        End If



        ' Show/hide direction buttons based on available exits
        btnNorth.Visibility = If(currentRoom.Exits.ContainsKey("North"), Visibility.Visible, Visibility.Collapsed)
        btnSouth.Visibility = If(currentRoom.Exits.ContainsKey("South"), Visibility.Visible, Visibility.Collapsed)
        btnEast.Visibility = If(currentRoom.Exits.ContainsKey("East"), Visibility.Visible, Visibility.Collapsed)
        btnWest.Visibility = If(currentRoom.Exits.ContainsKey("West"), Visibility.Visible, Visibility.Collapsed)
        btnLightSwitch.Visibility = If(currentRoom.Exits.ContainsKey("North"), Visibility.Visible, Visibility.Collapsed)
        btnLightSwitch.Visibility = If(currentRoom.Exits.ContainsKey("North"), Visibility.Visible, Visibility.Collapsed)
        ' Show enemy/NPC/item status
        If currentRoom.Enemy IsNot Nothing AndAlso currentRoom.Enemy.IsAlive() Then
            lblNpcName.Content = currentRoom.Enemy.Name
            lblEnemyStatus.Content = "Enemy present: " & currentRoom.Enemy.Name
            btnAttack.Visibility = Visibility.Visible
            imgEnemy.Visibility = Visibility.Visible
            txtNpcDialogue.Text = "No Escape! RAHHHHH!"
            'placing player and enemy in combat 
            imgPlayer.HorizontalAlignment = HorizontalAlignment.Left
            imgPlayer.Margin = New Thickness(40, 0, 0, 40)

            imgEnemy.HorizontalAlignment = HorizontalAlignment.Right
            imgEnemy.Margin = New Thickness(0, 0, 40, 40)
        Else
            lblEnemyStatus.Content = "Room is clear."
            'btnAttack.Visibility = Visibility.Collapsed
            imgEnemy.Visibility = Visibility.Collapsed

            imgPlayer.HorizontalAlignment = HorizontalAlignment.Center
            imgPlayer.Margin = New Thickness(0, 0, 0, 40)


        End If
    End Sub

    Sub UpdateInventoryDisplay() ' Had to declare this as a subroutine so I can call it from other places (like when player picks up loot)
        lstInventory.Items.Clear()
        For Each item As String In player.Inventory
            lstInventory.Items.Add(item)
        Next
    End Sub

    Private Sub UpdateHealthBars()
        ' Player health bar (a WPF ProgressBar named pbarPlayerHealth)
        pbarPlayerHealth.Value = player.Health
        pbarPlayerHealth.Maximum = player.MaxHealth
        lblPlayerHealth.Content = player.Health & " / " & player.MaxHealth

        ' Enemy health bar
        If currentRoom.Enemy IsNot Nothing Then
            pbarEnemyHealth.Value = Math.Max(0, currentRoom.Enemy.Health)
            pbarEnemyHealth.Maximum = currentRoom.Enemy.MaxHealth
        End If
    End Sub

    '--------------------------------------------------------------------------------------------------------------
    'Interactive UI Logic
    '--------------------------------------------------------------------------------------------------------------

    'Buttons
    Private Sub btnNorth_Click(sender As Object, e As RoutedEventArgs) Handles btnNorth.Click
        ' Check if the current room has a "North" exit; notice it has to specifically contain it as a string!
        If currentRoom.Exits.ContainsKey("North") Then
            Dim nextRoomName As String = currentRoom.Exits("North")
            currentRoom = gameRooms(nextRoomName)  ' gameRooms is a Dictionary of all rooms, so HOW DO I DECLARE A DICTIONARY?
            UpdateRoomDisplay()
            AddToLog("You moved north into " & currentRoom.Name & ".")
        Else
            AddToLog("There is no path to the north.")
        End If
    End Sub

    Private Sub btnSouth_Click(sender As Object, e As RoutedEventArgs) Handles btnSouth.Click
        If currentRoom.Exits.ContainsKey("South") Then
            Dim nextRoomName As String = currentRoom.Exits("South")
            currentRoom = gameRooms(nextRoomName)
            UpdateRoomDisplay()
            AddToLog("You moved south into " & currentRoom.Name & ".")
            AddToLog("Oh no, the doors have locked! I must find a key.")
        Else
            AddToLog("There is no path to the south.")
        End If
    End Sub

    Private Sub btnEast_Click(sender As Object, e As RoutedEventArgs) Handles btnEast.Click
        If currentRoom.Exits.ContainsKey("East") Then
            Dim nextRoomName As String = currentRoom.Exits("East")
            currentRoom = gameRooms(nextRoomName)
            UpdateRoomDisplay()
            AddToLog("You moved east into " & currentRoom.Name & ".")
        Else
            AddToLog("There is no path to the east.")
        End If
    End Sub

    Private Sub btnWest_Click(sender As Object, e As RoutedEventArgs) Handles btnWest.Click
        If currentRoom.Exits.ContainsKey("West") Then
            Dim nextRoomName As String = currentRoom.Exits("West")
            currentRoom = gameRooms(nextRoomName)
            UpdateRoomDisplay()
            AddToLog("You moved west into " & currentRoom.Name & ".")
        Else
            AddToLog("There is no path to the west.")
        End If
    End Sub

    Private Sub btnAttack_Click(sender As Object, e As RoutedEventArgs) Handles btnAttack.Click
        Dim enemy As Enemy = currentRoom.Enemy

        ' Player attacks first
        Dim playerDamage As Integer = player.Attack(enemy)
        AddToLog("You deal " & playerDamage & " damage to " & enemy.Name & "!")
        UpdateHealthBars()

        If Not enemy.IsAlive() Then
            AddToLog(enemy.Name & " has been defeated!")
            HandleEnemyDefeat(enemy)
            txtNpcDialogue.Text = "bleh"
            Return
        End If

        ' Enemy counter-attacks
        Dim enemyDamage As Integer = enemy.AttackPlayer(player)
        AddToLog(enemy.Name & " strikes back for " & enemyDamage & " damage!")
        UpdateHealthBars()

        If Not player.IsAlive() Then
            AddToLog("You have been defeated... Game Over.")
            ShowGameOver() ' needed to be declared
        End If
    End Sub


    Private Sub btnSave_Click(sender As Object, e As RoutedEventArgs) Handles btnSave.Click
        SaveGame()
    End Sub

    Private Sub btnLightSwitch_Click(sender As Object, e As RoutedEventArgs) Handles btnLightSwitch.Click
        lightsOn = Not lightsOn

        If lightsOn Then
            AddToLog("Lights turned ON . . . What is this place?")
            AddToLog("The faster I find a radio, the faster I can leave this hell hole.")
        Else
            AddToLog("Lights turned OFF")
        End If

        UpdateRoomDisplay()
    End Sub
    'things that appear or disappear according to the rooms

    Private Sub AddToLog(message As String)
        txtCombatLog.AppendText(vbCrLf & message)
        txtCombatLog.ScrollToEnd()
    End Sub


    '--------------------------------------------------------------------------------------------------------------
    'Main Character Logic
    '--------------------------------------------------------------------------------------------------------------



    'Main Character MOvement and other actions
    Private Sub Window_KeyDown(sender As Object, e As KeyEventArgs) Handles DeadPowerGame.KeyDown
        If e.Key = Key.Right Then
            rightKey = True
        End If

        If e.Key = Key.Left Then
            leftKey = True
        End If

        If e.Key = Key.Up Then
            upKey = True
        End If

        If e.Key = Key.Down Then
            downKey = True
        End If
    End Sub

    Private Sub Window_KeyUp(sender As Object, e As KeyEventArgs) Handles DeadPowerGame.KeyUp
        If e.Key = Key.Right Then
            rightKey = False
        End If

        If e.Key = Key.Left Then
            leftKey = False
        End If

        If e.Key = Key.Up Then
            upKey = False
        End If

        If e.Key = Key.Down Then
            downKey = False
        End If
    End Sub

    Private Sub MoveLeft()
        rectPlayer.X -= 2
        imgPlayer.Margin = New Thickness(imgPlayer.Margin.Left - 2, imgPlayer.Margin.Top, 0, 0)
    End Sub

    Private Sub MoveRight()
        rectPlayer.X += 2
        imgPlayer.Margin = New Thickness(imgPlayer.Margin.Left + 2, imgPlayer.Margin.Top, 0, 0)
    End Sub

    Private Sub MoveUp()
        rectPlayer.Y -= 2
        imgPlayer.Margin = New Thickness(imgPlayer.Margin.Left, imgPlayer.Margin.Top - 2, 0, 0)
    End Sub

    Private Sub MoveDown()
        rectPlayer.Y += 2
        imgPlayer.Margin = New Thickness(imgPlayer.Margin.Left, imgPlayer.Margin.Top + 2, 0, 0)
    End Sub

    'Main Character 




    '--------------------------------------------------------------------------------------------------------------
    'Enemy Logic
    '--------------------------------------------------------------------------------------------------------------

    'Enemy Defeat
    Private Sub HandleEnemyDefeat(enemy As Enemy)
        If enemy.LootDrop <> "" Then
            player.PickUpItem(enemy.LootDrop)
            AddToLog("You found: " & enemy.LootDrop)
            UpdateInventoryDisplay() ' needed to be declared
        End If
        btnAttack.Visibility = Visibility.Collapsed

        UpdateRoomDisplay()
        UpdateHealthBars()
        AddToLog("The room is now clear.")
    End Sub

    'Enemy Movement

    'Enemy Collision



End Class
