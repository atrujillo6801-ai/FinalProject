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

    Dim basePath As String = System.IO.Directory.GetCurrentDirectory()
    Dim relativePath As String = ""
    Dim fullPath As String = Path.Combine(basePath, relativePath)









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



        Dim southRoom As New Room()
        southRoom.Name = "South Key Room"
        southRoom.Description = "A glint catches your eye from the corner of the room."
        southRoom.Exits.Add("North", "East Dark Room")

        northRoom.Enemy = New Enemy("Zombie", 100, 10)



        'this adds the rooms we created to the dictionary of rooms, using the string of the room's name as the key. This allows us to easily look up any room by its name later on (like when we want to move to a new room).
        gameRooms.Add(entrance.Name, entrance)
        gameRooms.Add(northRoom.Name, northRoom)
        gameRooms.Add(eastRoom.Name, eastRoom)
        gameRooms.Add(southRoom.Name, southRoom)



        ' Set starting room
        currentRoom = entrance

        ' Update the screen
        UpdateRoomDisplay()
        UpdateHealthBars()
        UpdateInventoryDisplay()
        AddToLog("My plane was shot down. Maybe this building will provide the means to my escape.")



        AddHandler CompositionTarget.Rendering, AddressOf GameLoop


    End Sub

    Private Sub GameLoop()

        If leftKey Then CheckKeyToMove(MoveKey.Left)
        If rightKey Then CheckKeyToMove(MoveKey.Right)
        If upKey Then CheckKeyToMove(MoveKey.Up)
        If downKey Then CheckKeyToMove(MoveKey.Down)



    End Sub

    Private Sub CheckKeyToMove(isKeyMove As MoveKey)
        Select Case isKeyMove
            Case isKeyMove.Left
                MoveLeft()
            Case isKeyMove.Right
                MoveRight()

            Case Else


        End Select
    End Sub





    'Room Logic

    'Loading a game

    'Saving a game

    'GameOver

    'updating functions defined here




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






    'things that appear or disappear according to the rooms
    Private Sub UpdateRoomDisplay()
        lblRoomName.Content = currentRoom.Name


        ' Change background based on lights




        ' Show/hide direction buttons based on available exits
        btnNorth.Visibility = If(currentRoom.Exits.ContainsKey("North"), Visibility.Visible, Visibility.Collapsed)
        btnSouth.Visibility = If(currentRoom.Exits.ContainsKey("South"), Visibility.Visible, Visibility.Collapsed)
        btnEast.Visibility = If(currentRoom.Exits.ContainsKey("East"), Visibility.Visible, Visibility.Collapsed)
        btnWest.Visibility = If(currentRoom.Exits.ContainsKey("West"), Visibility.Visible, Visibility.Collapsed)
        btnLightSwitch.Visibility = If(currentRoom.Exits.ContainsKey("North"), Visibility.Visible, Visibility.Collapsed)
        btnLightSwitch.Visibility = If(currentRoom.Exits.ContainsKey("North"), Visibility.Visible, Visibility.Collapsed)
        ' Show enemy/NPC/item status
        If currentRoom.Enemy IsNot Nothing AndAlso currentRoom.Enemy.IsAlive() Then
            lblEnemyStatus.Content = "Enemy present: " & currentRoom.Enemy.Name
            ' btnAttack.Visibility = Visibility.Visible
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

    Private Sub AddToLog(message As String)
        txtCombatLog.AppendText(vbCrLf & message)
        txtCombatLog.ScrollToEnd()
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
        imgPlayer.Margin = New Thickness(imgPlayer.Margin.Left - 2, imgPlayer.Margin.Top, 0, 0)
    End Sub

    Private Sub MoveRight()
        imgPlayer.Margin = New Thickness(imgPlayer.Margin.Left + 2, imgPlayer.Margin.Top, 0, 0)
    End Sub

    Private Sub MoveUp()
        imgPlayer.Margin = New Thickness(imgPlayer.Margin.Left, imgPlayer.Margin.Top - 2, 0, 0)
    End Sub

    Private Sub MoveDown()
        imgPlayer.Margin = New Thickness(imgPlayer.Margin.Left, imgPlayer.Margin.Top + 2, 0, 0)
    End Sub

    'Main Character 




    '--------------------------------------------------------------------------------------------------------------
    'Enemy Logic
    '--------------------------------------------------------------------------------------------------------------

    'Enemy Defeat

    'Enemy Movement

    'Enemy Collision



End Class
