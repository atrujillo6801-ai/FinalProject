Imports Microsoft.VisualBasic
Imports System.IO
Imports Newtonsoft.Json



Class MainWindow
    '--------------------------------------------------------------------------------------------------------------
    'Declarations for Variables that are used across the entire code 
    '--------------------------------------------------------------------------------------------------------------
    Enum MoveKey
        A = 0
        D = 1
        W = 2
        S = 3
        Space = 4
    End Enum

    Dim currentRoom As Room
    Dim gameRooms As Dictionary(Of String, Room)
    Dim player As Player
    Dim lightsOn As Boolean = False
    Dim leftKey As Boolean
    Dim rightKey As Boolean
    Dim upKey As Boolean
    Dim downKey As Boolean
    Dim spaceKey As Boolean
    Dim speed As Integer
    Dim zombieSpeed As Double = 0.8
    Dim zombieTouchDamage As Integer = 2
    Dim zombieCanDamage As Boolean = True
    Dim zombieDamageCooldown As Integer = 0
    Dim zombieDamageCooldownMax As Integer = 60
    Private rectPlayer As Rect
    Private rectNorth As Rect
    Private rectSouth As Rect
    Private rectWest As Rect
    Private rectEast As Rect

    Private collisionHandled As Boolean = False
    Private attackPressed As Boolean = False
    Private noExitBounce As Boolean = False

    Dim basePath As String = AppDomain.CurrentDomain.BaseDirectory
    Dim relativePath As String = ""
    Dim fullPath As String = Path.Combine(basePath, relativePath)









    '--------------------------------------------------------------------------------------------------------------
    'Main Game Logic
    '--------------------------------------------------------------------------------------------------------------
    Public Sub New()
        InitializeComponent()

        gameRooms = New Dictionary(Of String, Room)

        player = New Player("Sam Stones")
        lblPlayerName.Content = player.Name

        Dim entrance As New Room()
        entrance.Name = "West Entrance Hall"
        entrance.Description = "Looks like a deserted building. I wonder if I can find a radio inside? (Use your keyboard keys to explore rooms)."
        entrance.Exits.Add("East", "East Dark Room")

        Dim eastRoom As New Room()
        eastRoom.Name = "East Dark Room"
        eastRoom.Description = "Looks like the power is down. Maybe there is a breaker or an auxilary generator somewhere."
        eastRoom.Exits.Add("West", "West Entrance Hall")
        eastRoom.Exits.Add("North", "North Zombie Room")
        eastRoom.Enemy = New Enemy("Zombie", 100, 2)
        eastRoom.Enemy.LootDrop = "Rusty Key"

        Dim northRoom As New Room()
        northRoom.Name = "North Zombie Room"
        northRoom.Description = "You find an unexpected guest."
        northRoom.Exits.Add("South", "East Dark Room")
        northRoom.Enemy = New Enemy("Zombie", 100, 2)
        northRoom.Enemy.LootDrop = "Radio"

        gameRooms.Add(entrance.Name, entrance)
        gameRooms.Add(northRoom.Name, northRoom)
        gameRooms.Add(eastRoom.Name, eastRoom)

        currentRoom = entrance

        rectPlayer = imageToRect(imgPlayer)
        rectNorth = imageToRect(imgNorth)
        rectEast = imageToRect(imgEast)
        rectSouth = imageToRect(imgSouth)
        rectWest = imageToRect(imgWest)

        UpdateRoomDisplay()
        UpdateHealthBars()
        UpdateInventoryDisplay()
        UpdatePowerSwitchImage()
        AddToLog("My plane was shot down. Maybe this building will provide the means to my escape.")


        Me.Focus()

        AddHandler CompositionTarget.Rendering, AddressOf GameLoop

        Dim leftOffset As Double = 10
        Dim topOffset As Double = 10
        Dim roomWidth As Double = mainCanvas.ActualWidth
        Dim roomHeight As Double = mainCanvas.ActualHeight
        If roomWidth = 0 Then roomWidth = 400
        If roomHeight = 0 Then roomHeight = 300



    End Sub
    Private Function imageToRect(sprite As Image) As Rect
        Dim x As Double = Canvas.GetLeft(sprite)
        Dim y As Double = Canvas.GetTop(sprite)

        If Double.IsNaN(x) Then x = 0
        If Double.IsNaN(y) Then y = 0

        Return New Rect(x, y, sprite.Width, sprite.Height)
    End Function
    Private Sub GameLoop()
        Me.Focus()

        If leftKey Then CheckKeyToMove(MoveKey.A)
        If rightKey Then CheckKeyToMove(MoveKey.D)
        If upKey Then CheckKeyToMove(MoveKey.W)
        If downKey Then CheckKeyToMove(MoveKey.S)

        MoveZombieTowardPlayer()
        CheckZombiePlayerCollision()
        UpdateZombieDamageCooldown()

        CheckCollision()
    End Sub

    Private Sub CheckKeyToMove(isKeyMove As MoveKey)
        Select Case isKeyMove
            Case MoveKey.A
                MoveLeft()
            Case MoveKey.D
                MoveRight()
            Case MoveKey.W
                MoveUp()
            Case MoveKey.S
                MoveDown()
            Case MoveKey.Space
                btnAttack_Click(Nothing, Nothing)
            Case Else

        End Select
    End Sub

    Private Function CollisionTestWalls(objA As Rect, wallObj As Rect) As Boolean
        Return objA.IntersectsWith(wallObj)
    End Function

    Sub CheckCollision()

        If collisionHandled Then Exit Sub

        If CollisionTestWalls(rectPlayer, rectNorth) Then
            collisionHandled = True

            If Not currentRoom.Exits.ContainsKey("North") Then
                Canvas.SetTop(imgPlayer, Canvas.GetTop(imgPlayer) + 10)
                rectPlayer = imageToRect(imgPlayer)
                collisionHandled = False
            End If
            btnNorth_Click(Nothing, Nothing)
        ElseIf CollisionTestWalls(rectPlayer, rectEast) Then
            collisionHandled = True

            If Not currentRoom.Exits.ContainsKey("East") Then
                Canvas.SetLeft(imgPlayer, Canvas.GetLeft(imgPlayer) - 10)
                rectPlayer = imageToRect(imgPlayer)
                collisionHandled = False
            End If
            btnEast_Click(Nothing, Nothing)
        ElseIf CollisionTestWalls(rectPlayer, rectSouth) Then
            collisionHandled = True

            If Not currentRoom.Exits.ContainsKey("South") Then
                Canvas.SetTop(imgPlayer, Canvas.GetTop(imgPlayer) - 10)
                rectPlayer = imageToRect(imgPlayer)
                collisionHandled = False
            End If
            btnSouth_Click(Nothing, Nothing)
        ElseIf CollisionTestWalls(rectPlayer, rectWest) Then
            collisionHandled = True

            If Not currentRoom.Exits.ContainsKey("West") Then
                Canvas.SetLeft(imgPlayer, Canvas.GetLeft(imgPlayer) + 10)
                rectPlayer = imageToRect(imgPlayer)
                collisionHandled = False
            End If
            btnWest_Click(Nothing, Nothing)
        Else
            collisionHandled = False
        End If
    End Sub

    Private Sub LoadGame()
        Dim savePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\save.json")

        If Not File.Exists(savePath) Then
            AddToLog("No save file found. Starting new game.")
            Return
        End If

        Dim json As String = File.ReadAllText(savePath)
        Dim saveData As SaveData = JsonConvert.DeserializeObject(Of SaveData)(json)

        player = New Player(saveData.Name)
        player.Health = saveData.Health
        player.MaxHealth = saveData.MaxHealth
        player.AttackPower = saveData.AttackPower
        player.Gold = saveData.Gold
        player.Inventory = saveData.Inventory

        currentRoom = gameRooms(saveData.CurrentRoom)

        UpdateRoomDisplay()
        UpdateHealthBars()
        UpdateInventoryDisplay()
        AddToLog("Save loaded. Welcome back, " & player.Name & "!")
    End Sub

    Private Sub SaveGame()
        Dim saveData As New SaveData
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

    Sub ShowGameOver()
        MessageBox.Show("Game Over! Sam Stones has perished.")
        Application.Current.Shutdown()
    End Sub

    Private Sub UpdateRoomDisplay()
        lblRoomName.Content = currentRoom.Name


        ' Change background based on lights
        If currentRoom.Name = "West Entrance Hall" Then
            relativePath = "Assets\images\RoomLight.png"
            mainCanvas.Background = New ImageBrush(New BitmapImage(New Uri(basePath + "/" + relativePath)))
        ElseIf currentRoom.Name = "North Zombie Room" And lightsOn Then
            relativePath = "Assets\images\RadioRoom.png"
            mainCanvas.Background = New ImageBrush(New BitmapImage(New Uri(basePath + "/" + relativePath)))
        ElseIf currentRoom.Name = "East Dark Room" And lightsOn Then
            relativePath = "Assets\images\PowerRoom.png"
            mainCanvas.Background = New ImageBrush(New BitmapImage(New Uri(basePath + "/" + relativePath)))
        Else
            relativePath = "Assets\images\RoomDark.png"
            mainCanvas.Background = New ImageBrush(New BitmapImage(New Uri(basePath + "/" + relativePath)))
        End If

        btnNorth.Visibility = If(currentRoom.Exits.ContainsKey("North"), Visibility.Visible, Visibility.Collapsed)
        btnSouth.Visibility = If(currentRoom.Exits.ContainsKey("South"), Visibility.Visible, Visibility.Collapsed)
        btnEast.Visibility = If(currentRoom.Exits.ContainsKey("East"), Visibility.Visible, Visibility.Collapsed)
        btnWest.Visibility = If(currentRoom.Exits.ContainsKey("West"), Visibility.Visible, Visibility.Collapsed)

        If currentRoom.Name = "East Dark Room" Then
            btnLightSwitch.Visibility = Visibility.Visible
        Else
            btnLightSwitch.Visibility = Visibility.Collapsed
        End If

        'Placing player in combat

        If currentRoom.Enemy IsNot Nothing AndAlso currentRoom.Enemy.IsAlive() Then
            btnAttack.Visibility = Visibility.Visible
            imgEnemy.Visibility = Visibility.Visible

            Canvas.SetLeft(imgPlayer, 40)
            Canvas.SetTop(imgPlayer, 250)
            rectPlayer = imageToRect(imgPlayer)

            Canvas.SetLeft(imgEnemy, 528)
            Canvas.SetTop(imgEnemy, 273)
        Else
            btnAttack.Visibility = Visibility.Collapsed
            imgEnemy.Visibility = Visibility.Collapsed

            Canvas.SetLeft(imgPlayer, 180)
            Canvas.SetTop(imgPlayer, 250)
            rectPlayer = imageToRect(imgPlayer)
        End If

        rectNorth = imageToRect(imgNorth)
        rectEast = imageToRect(imgEast)
        rectSouth = imageToRect(imgSouth)
        rectWest = imageToRect(imgWest)
        rectPlayer = imageToRect(imgPlayer)

        collisionHandled = False
    End Sub

    Sub UpdateInventoryDisplay() ' Had to declare this as a subroutine so I can call it from other places (like when player picks up loot)
        lstInventory.Items.Clear()
        For Each item As String In player.Inventory
            lstInventory.Items.Add(item)
        Next

    End Sub

    Private Sub UpdateRadioVisibility()
        If currentRoom.Name = "North Zombie Room" AndAlso currentRoom.Enemy IsNot Nothing AndAlso Not currentRoom.Enemy.IsAlive() Then
            imgRadio.Visibility = Visibility.Visible
        Else
            imgRadio.Visibility = Visibility.Collapsed
        End If
    End Sub

    Private Sub UpdateHealthBars()
        pbarPlayerHealth.Value = player.Health
        pbarPlayerHealth.Maximum = player.MaxHealth
        lblPlayerHealth.Content = player.Health & " / " & player.MaxHealth

        If currentRoom.Enemy IsNot Nothing Then
            pbarEnemyHealth.Value = Math.Max(0, currentRoom.Enemy.Health)
            pbarEnemyHealth.Maximum = currentRoom.Enemy.MaxHealth
        Else
            pbarEnemyHealth.Value = 0
        End If
    End Sub

    '--------------------------------------------------------------------------------------------------------------
    'Interactive UI Logic
    '--------------------------------------------------------------------------------------------------------------

    'Buttons
    Private Sub btnNorth_Click(sender As Object, e As RoutedEventArgs) Handles btnNorth.Click
        If currentRoom.Exits.ContainsKey("North") Then
            Dim nextRoomName As String = currentRoom.Exits("North")
            currentRoom = gameRooms(nextRoomName)  ' gameRooms is a Dictionary of all rooms, so HOW DO I DECLARE A DICTIONARY?
            UpdateRoomDisplay()
            AddToLog("You moved north into " & currentRoom.Name & ".")
        Else
            AddToLog("There is no door to the north.")
            Me.Focus()
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
            AddToLog("There is no door to the south.")
        End If
        Me.Focus()
    End Sub

    Private Sub btnEast_Click(sender As Object, e As RoutedEventArgs) Handles btnEast.Click
        If currentRoom.Exits.ContainsKey("East") Then
            Dim nextRoomName As String = currentRoom.Exits("East")
            currentRoom = gameRooms(nextRoomName)
            UpdateRoomDisplay()
            AddToLog("You moved east into " & currentRoom.Name & ".")
        Else
            AddToLog("There is no door to the east.")
        End If
        Me.Focus()
    End Sub

    Private Sub btnWest_Click(sender As Object, e As RoutedEventArgs) Handles btnWest.Click
        If currentRoom.Exits.ContainsKey("West") Then
            Dim nextRoomName As String = currentRoom.Exits("West")
            currentRoom = gameRooms(nextRoomName)
            UpdateRoomDisplay()
            AddToLog("You moved west into " & currentRoom.Name & ".")
        Else
            AddToLog("There is no door to the west.")
        End If
        Me.Focus()
    End Sub

    Private Sub btnAttack_Click(sender As Object, e As RoutedEventArgs) Handles btnAttack.Click
        Dim enemy As Enemy = currentRoom.Enemy

        If enemy IsNot Nothing AndAlso btnAttack.Visibility = Visibility.Visible Then
            Dim playerDamage As Integer = player.Attack(enemy)
            AddToLog("You deal " & playerDamage & " damage to " & enemy.Name & "!")
            UpdateHealthBars()

            If Not enemy.IsAlive() Then
                AddToLog(enemy.Name & " has been defeated!")
                HandleEnemyDefeat(enemy)
                Return
            End If

            Dim enemyDamage As Integer = enemy.AttackPlayer(player)
            AddToLog(enemy.Name & " strikes back for " & enemyDamage & " damage!")
            UpdateHealthBars()

            If Not player.IsAlive() Then
                AddToLog("Never thought it would end this way.")
                ShowGameOver()
            End If
        End If
        ' -DrManzo (FIX 2 - return focus after button click) Same reason as direction buttons — prevents spacebar from re-triggering btnAttack on the next press instead of routing through Window_KeyDown.
        Me.Focus()
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
            AddToLog("It's too dark to see anything")
        End If

        UpdateRoomDisplay()
        UpdatePowerSwitchImage()
        Me.Focus()
    End Sub

    Private Sub UpdatePowerSwitchImage()
        Dim switchPath As String

        If lightsOn Then
            switchPath = Path.Combine(basePath, "Assets\images\LightSwitchOn.png")
        Else
            switchPath = Path.Combine(basePath, "Assets\images\LightSwitchOff.png")
        End If

        btnLightSwitch.Content = ""

        Dim switchBrush As New ImageBrush()
        switchBrush.ImageSource = New BitmapImage(New Uri(switchPath, UriKind.Absolute))
        switchBrush.Stretch = Stretch.Uniform

        btnLightSwitch.Background = switchBrush
    End Sub



    Private Sub AddToLog(message As String)
        If txtCombatLog Is Nothing Then Exit Sub
        If txtCombatLog.Text = "" Then
            txtCombatLog.AppendText(message)
        Else
            txtCombatLog.AppendText(vbCrLf & message)
        End If
        txtCombatLog.ScrollToEnd()
    End Sub


    '--------------------------------------------------------------------------------------------------------------
    'Main Character Logic
    '--------------------------------------------------------------------------------------------------------------



    'Main Character MOvement and other actions
    Private Sub Window_KeyDown(sender As Object, e As KeyEventArgs) Handles DeadPowerGame.KeyDown
        If e.Key = Key.D Then rightKey = True
        If e.Key = Key.A Then leftKey = True
        If e.Key = Key.W Then upKey = True
        If e.Key = Key.S Then downKey = True

        If e.Key = Key.Space Then
            spaceKey = True
            ' -DrManzo (spacebar one-shot attack) Fire attack once when space is first pressed using attackPressed flag. Without this the game loop called attack every render frame while space was held.
            If Not attackPressed Then
                attackPressed = True
                CheckKeyToMove(MoveKey.Space)
            End If
        End If
    End Sub

    Private Sub Window_KeyUp(sender As Object, e As KeyEventArgs) Handles DeadPowerGame.KeyUp
        If e.Key = Key.D Then rightKey = False
        If e.Key = Key.A Then leftKey = False
        If e.Key = Key.W Then upKey = False
        If e.Key = Key.S Then downKey = False

        If e.Key = Key.Space Then
            spaceKey = False
            attackPressed = False
        End If
    End Sub

    Private Sub MoveLeft()
        Dim newX As Double = Math.Max(0, Canvas.GetLeft(imgPlayer) - 2)
        Canvas.SetLeft(imgPlayer, newX)
        rectPlayer = imageToRect(imgPlayer)
    End Sub
    Private Sub MoveRight()
        Dim newX As Double = Math.Min(mainCanvas.ActualWidth - imgPlayer.Width, Canvas.GetLeft(imgPlayer) + 2)
        Canvas.SetLeft(imgPlayer, newX)
        rectPlayer = imageToRect(imgPlayer)
    End Sub

    Private Sub MoveUp()
        Dim newY As Double = Math.Max(0, Canvas.GetTop(imgPlayer) - 2)
        Canvas.SetTop(imgPlayer, newY)
        rectPlayer = imageToRect(imgPlayer)
    End Sub

    Private Sub MoveDown()
        Dim newY As Double = Math.Min(mainCanvas.ActualHeight - imgPlayer.Height, Canvas.GetTop(imgPlayer) + 2)
        Canvas.SetTop(imgPlayer, newY)
        rectPlayer = imageToRect(imgPlayer)
    End Sub


    '--------------------------------------------------------------------------------------------------------------
    'Enemy Logic
    '--------------------------------------------------------------------------------------------------------------

    'Enemy Defeat
    Private Sub HandleEnemyDefeat(enemy As Enemy)
        If enemy.LootDrop <> "" Then

            If Not player.Inventory.Contains(enemy.LootDrop) Then
                player.PickUpItem(enemy.LootDrop)
                AddToLog("You found: " & enemy.LootDrop)
                UpdateInventoryDisplay()

                If enemy.LootDrop = "Radio" Then
                    AddToLog("A long distance radio!")
                    AddToLog("There is static crackling through the speaker. I finally have a way to call for rescue.")
                End If
            End If

        End If

        btnAttack.Visibility = Visibility.Collapsed
        UpdateRoomDisplay()
        UpdateHealthBars()
        AddToLog("The room is now clear.")
    End Sub

    Private Sub MoveZombieTowardPlayer()
        If currentRoom.Enemy Is Nothing Then Return
        If Not currentRoom.Enemy.IsAlive() Then Return
        If imgEnemy.Visibility <> Visibility.Visible Then Return

        Dim zombieX As Double = Canvas.GetLeft(imgEnemy)
        Dim zombieY As Double = Canvas.GetTop(imgEnemy)

        Dim playerX As Double = Canvas.GetLeft(imgPlayer)
        Dim playerY As Double = Canvas.GetTop(imgPlayer)

        If Double.IsNaN(zombieX) Then zombieX = 0
        If Double.IsNaN(zombieY) Then zombieY = 0
        If Double.IsNaN(playerX) Then playerX = 0
        If Double.IsNaN(playerY) Then playerY = 0

        If zombieX < playerX Then
            Canvas.SetLeft(imgEnemy, zombieX + zombieSpeed)
        ElseIf zombieX > playerX Then
            Canvas.SetLeft(imgEnemy, zombieX - zombieSpeed)
        End If

        If zombieY < playerY Then
            Canvas.SetTop(imgEnemy, zombieY + zombieSpeed)
        ElseIf zombieY > playerY Then
            Canvas.SetTop(imgEnemy, zombieY - zombieSpeed)
        End If
    End Sub

    Private Sub CheckZombiePlayerCollision()
        If currentRoom.Enemy Is Nothing Then Return
        If Not currentRoom.Enemy.IsAlive() Then Return
        If imgEnemy.Visibility <> Visibility.Visible Then Return

        Dim playerRect As Rect = imageToRect(imgPlayer)
        Dim zombieRect As Rect = imageToRect(imgEnemy)

        If playerRect.IntersectsWith(zombieRect) Then
            If zombieCanDamage Then
                player.Health -= zombieTouchDamage

                AddToLog("The zombie claws Sam Stones for " & zombieTouchDamage & " damage!")
                UpdateHealthBars()

                zombieCanDamage = False
                zombieDamageCooldown = zombieDamageCooldownMax

                If Not player.IsAlive() Then
                    AddToLog("Sam Stones has been overwhelmed by the zombie...")
                    ShowGameOver()
                End If
            End If
        End If
    End Sub

    Private Sub UpdateZombieDamageCooldown()
        If zombieCanDamage = False Then
            zombieDamageCooldown -= 1

            If zombieDamageCooldown <= 0 Then
                zombieCanDamage = True
            End If
        End If
    End Sub


End Class
