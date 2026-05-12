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

    Private rectPlayer As Rect
    Private rectNorth As Rect
    Private rectSouth As Rect
    Private rectWest As Rect
    Private rectEast As Rect

    ' -DrManzo (collisionHandled flag) Prevents collision from firing every render frame while the player is still touching a wall, which was causing repeated room triggers and log spam.
    Private collisionHandled As Boolean = False

    ' -DrManzo (attackPressed flag) Makes the spacebar attack fire once per key press instead of every frame while the key is held.
    Private attackPressed As Boolean = False

    ' -DrManzo (FIX 1 - noExitBounce flag) When the player hits a wall with no exit, we need to push them back so they are no longer touching the wall rect. Without this, collisionHandled stays True forever and no future collisions register.
    Private noExitBounce As Boolean = False

    Dim basePath As String = System.IO.Directory.GetCurrentDirectory()
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
        eastRoom.Enemy = New Enemy("Zombie", 100, 10)
        eastRoom.Enemy.LootDrop = "Rusty Key"

        Dim northRoom As New Room()
        northRoom.Name = "North Zombie Room"
        northRoom.Description = "You find an unexpected guest."
        northRoom.Exits.Add("South", "East Dark Room")
        northRoom.Enemy = New Enemy("Zombie", 100, 10)

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
        Return New Rect(Canvas.GetLeft(sprite), Canvas.GetTop(sprite), sprite.Width, sprite.Height)
    End Function

    Private Sub GameLoop()
        Me.Focus()
        If leftKey Then CheckKeyToMove(MoveKey.A)
        If rightKey Then CheckKeyToMove(MoveKey.D)
        If upKey Then CheckKeyToMove(MoveKey.W)
        If downKey Then CheckKeyToMove(MoveKey.S)
        ' -DrManzo (removed spaceKey from game loop) Space attack is now handled in Window_KeyDown so it fires once per press, not every render frame.

        CheckCollision()
        ' -DrManzo (removed dead collision calls) These four calls returned a value but it was never used, so they did nothing. CheckCollision() handles all wall checks.
    End Sub

    Private Sub CheckKeyToMove(isKeyMove As MoveKey)
        Select Case isKeyMove
            ' -DrManzo (BC42025 fix) Changed all Case lines from isKeyMove.X to MoveKey.X because MoveKey is an Enum type, not an instance variable. Using the parameter caused BC42025 warnings.
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


    'Room Logic
    Private Function CollisionTestWalls(objA As Rect, wallObj As Rect) As Boolean
        Return objA.IntersectsWith(wallObj)
    End Function

    Sub CheckCollision()
        ' -DrManzo (collision guard) If a collision was already handled, exit early so the same wall contact does not fire room logic and AddToLog every frame.
        If collisionHandled Then Exit Sub

        If CollisionTestWalls(rectPlayer, rectNorth) Then
            collisionHandled = True
            ' -DrManzo (FIX 1 - no exit bounce North) If there is no North exit, push the player back down by 10px so they leave the wall rect. This clears collisionHandled on the next frame and lets future collisions register normally.
            If Not currentRoom.Exits.ContainsKey("North") Then
                Canvas.SetTop(imgPlayer, Canvas.GetTop(imgPlayer) + 10)
                rectPlayer = imageToRect(imgPlayer)
                collisionHandled = False
            End If
            btnNorth_Click(Nothing, Nothing)
            AddToLog("You bumped into a wall to the north.")
        ElseIf CollisionTestWalls(rectPlayer, rectEast) Then
            collisionHandled = True
            ' -DrManzo (FIX 1 - no exit bounce East) Same reason as North — push player left so they exit the wall rect and collisions can fire again.
            If Not currentRoom.Exits.ContainsKey("East") Then
                Canvas.SetLeft(imgPlayer, Canvas.GetLeft(imgPlayer) - 10)
                rectPlayer = imageToRect(imgPlayer)
                collisionHandled = False
            End If
            btnEast_Click(Nothing, Nothing)
            AddToLog("You bumped into a wall to the east.")
        ElseIf CollisionTestWalls(rectPlayer, rectSouth) Then
            collisionHandled = True
            ' -DrManzo (FIX 1 - no exit bounce South) Push player up so they exit the wall rect when there is no South exit.
            If Not currentRoom.Exits.ContainsKey("South") Then
                Canvas.SetTop(imgPlayer, Canvas.GetTop(imgPlayer) - 10)
                rectPlayer = imageToRect(imgPlayer)
                collisionHandled = False
            End If
            btnSouth_Click(Nothing, Nothing)
            AddToLog("You bumped into a wall to the south.")
        ElseIf CollisionTestWalls(rectPlayer, rectWest) Then
            collisionHandled = True
            ' -DrManzo (FIX 1 - no exit bounce West) Push player right so they exit the wall rect when there is no West exit.
            If Not currentRoom.Exits.ContainsKey("West") Then
                Canvas.SetLeft(imgPlayer, Canvas.GetLeft(imgPlayer) + 10)
                rectPlayer = imageToRect(imgPlayer)
                collisionHandled = False
            End If
            btnWest_Click(Nothing, Nothing)
            AddToLog("You bumped into a wall to the West.")
        Else
            ' -DrManzo (collision reset) Reset the flag once the player is no longer touching a wall so the next valid collision can be detected.
            collisionHandled = False
        End If
    End Sub

    'Loading a game
    Private Sub LoadGame()
        Dim savePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\save.json")

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

    'Saving a game
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
        Dim savePath As String = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data\\save.json")
        File.WriteAllText(savePath, json)
        AddToLog("Game saved successfully.")
    End Sub

    'GameOver
    Sub ShowGameOver()
        MessageBox.Show("Game Over! Thanks for playing.")
        Application.Current.Shutdown()
    End Sub

    'Updating functions defined here
    Private Sub UpdateRoomDisplay()
        lblRoomName.Content = currentRoom.Name

        ' -DrManzo (IMPROVE 3 - show room description) currentRoom.Description was written for every room but never displayed. This shows it in lblRoomDescription so the player can read where they are.
        lblRoomDescription.Content = currentRoom.Description

        If currentRoom.Name = "West Entrance Hall" Then
            relativePath = "Assets\\images\\RoomLight.png"
            mainCanvas.Background = New ImageBrush(New BitmapImage(New Uri(basePath + "/" + relativePath)))
        ElseIf currentRoom.Name = "North Zombie Room" And lightsOn Then
            relativePath = "Assets\\images\\RadioRoom.png"
            mainCanvas.Background = New ImageBrush(New BitmapImage(New Uri(basePath + "/" + relativePath)))
        ElseIf currentRoom.Name = "East Dark Room" And lightsOn Then
            relativePath = "Assets\\images\\PowerRoom.png"
            mainCanvas.Background = New ImageBrush(New BitmapImage(New Uri(basePath + "/" + relativePath)))
        Else
            relativePath = "Assets\\images\\RoomDark.png"
            mainCanvas.Background = New ImageBrush(New BitmapImage(New Uri(basePath + "/" + relativePath)))
        End If

        btnNorth.Visibility = If(currentRoom.Exits.ContainsKey("North"), Visibility.Visible, Visibility.Collapsed)
        btnSouth.Visibility = If(currentRoom.Exits.ContainsKey("South"), Visibility.Visible, Visibility.Collapsed)
        btnEast.Visibility = If(currentRoom.Exits.ContainsKey("East"), Visibility.Visible, Visibility.Collapsed)
        btnWest.Visibility = If(currentRoom.Exits.ContainsKey("West"), Visibility.Visible, Visibility.Collapsed)
        btnLightSwitch.Visibility = If(currentRoom.Exits.ContainsKey("North"), Visibility.Visible, Visibility.Collapsed)

        If currentRoom.Enemy IsNot Nothing AndAlso currentRoom.Enemy.IsAlive() Then
            btnAttack.Visibility = Visibility.Visible
            imgEnemy.Visibility = Visibility.Visible

            ' -DrManzo (Canvas combat positioning) Use Canvas.SetLeft/SetTop instead of Margin so the player sprite stays on the Canvas coordinate system that collision rectangles use.
            Canvas.SetLeft(imgPlayer, 40)
            Canvas.SetTop(imgPlayer, 250)
            rectPlayer = imageToRect(imgPlayer)

            Canvas.SetLeft(imgEnemy, 528)
            Canvas.SetTop(imgEnemy, 273)
        Else
            btnAttack.Visibility = Visibility.Collapsed
            imgEnemy.Visibility = Visibility.Collapsed

            ' -DrManzo (Canvas default positioning) Reset player to center using Canvas coordinates instead of Margin so movement and collision stay in sync after room changes.
            Canvas.SetLeft(imgPlayer, 180)
            Canvas.SetTop(imgPlayer, 250)
            rectPlayer = imageToRect(imgPlayer)
        End If

        ' -DrManzo (wall rect refresh) Rebuild all wall and player rectangles after every room display update so collision is always reading current on-screen positions.
        rectNorth = imageToRect(imgNorth)
        rectEast = imageToRect(imgEast)
        rectSouth = imageToRect(imgSouth)
        rectWest = imageToRect(imgWest)
        rectPlayer = imageToRect(imgPlayer)

        ' -DrManzo (collision reset on room change) Clear the collision flag whenever we enter a new room so the player can trigger doorways normally.
        collisionHandled = False
    End Sub

    Sub UpdateInventoryDisplay()
        lstInventory.Items.Clear()
        For Each item As String In player.Inventory
            lstInventory.Items.Add(item)
        Next
    End Sub

    Private Sub UpdateHealthBars()
        pbarPlayerHealth.Value = player.Health
        pbarPlayerHealth.Maximum = player.MaxHealth
        lblPlayerHealth.Content = player.Health & " / " & player.MaxHealth

        If currentRoom.Enemy IsNot Nothing Then
            pbarEnemyHealth.Value = Math.Max(0, currentRoom.Enemy.Health)
            pbarEnemyHealth.Maximum = currentRoom.Enemy.MaxHealth
        Else
            ' -DrManzo (IMPROVE 2 - clear enemy health bar) When there is no enemy in the room, reset the bar to 0. Without this it holds the last enemy's value and shows a stale red bar in empty rooms.
            pbarEnemyHealth.Value = 0
        End If
    End Sub

    '--------------------------------------------------------------------------------------------------------------
    'Interactive UI Logic
    '--------------------------------------------------------------------------------------------------------------

    Private Sub btnNorth_Click(sender As Object, e As RoutedEventArgs) Handles btnNorth.Click
        If currentRoom.Exits.ContainsKey("North") Then
            Dim nextRoomName As String = currentRoom.Exits("North")
            currentRoom = gameRooms(nextRoomName)
            UpdateRoomDisplay()
            AddToLog("You moved north into " & currentRoom.Name & ".")
        Else
            AddToLog("There is no path to the north.")
        End If
        ' -DrManzo (FIX 2 - return focus after button click) WPF gives keyboard focus to whichever button was last clicked. This causes spacebar to re-fire that button instead of going to Window_KeyDown. Returning focus to the window after every direction click fixes this.
        Me.Focus()
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
        ' -DrManzo (FIX 2 - return focus after button click) Same reason as btnNorth — take focus back so spacebar routes to the window, not this button.
        Me.Focus()
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
        ' -DrManzo (FIX 2 - return focus after button click) Same reason as btnNorth.
        Me.Focus()
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
        ' -DrManzo (FIX 2 - return focus after button click) Same reason as btnNorth.
        Me.Focus()
    End Sub

    Private Sub btnAttack_Click(sender As Object, e As RoutedEventArgs) Handles btnAttack.Click
        Dim enemy As Enemy = currentRoom.Enemy
        ' -DrManzo (attack guard) Only run attack logic when an enemy exists and the attack button is visible so spacebar cannot trigger attacks in rooms with no enemy.
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
                AddToLog("You have been defeated... Game Over.")
                ShowGameOver()
            End If
        End If
        ' -DrManzo (FIX 2 - return focus after button click) Same reason as direction buttons — prevents spacebar from re-triggering btnAttack on the next press instead of routing through Window_KeyDown.
        Me.Focus()
    End Sub

    Private Sub btnSave_Click(sender As Object, e As RoutedEventArgs) Handles btnSave.Click
        SaveGame()
        ' -DrManzo (FIX 2 - return focus after button click) Prevents spacebar from triggering Save repeatedly after the button is clicked.
        Me.Focus()
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
        ' -DrManzo (FIX 2 - return focus after button click) This is the exact button your lead described — clicking the light switch then pressing space was toggling the switch again instead of attacking. Me.Focus() returns control to the window keyboard handler.
        Me.Focus()
    End Sub

    Private Sub AddToLog(message As String)
        ' -DrManzo (log fix) Guard against a null control, then append cleanly. The leading blank line only gets added when there is already text so the log does not start with an empty line.
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
            ' -DrManzo (spacebar release) Reset attackPressed so the next space press fires another attack.
            attackPressed = False
        End If
    End Sub

    Private Sub MoveLeft()
        ' -DrManzo (IMPROVE 1 - canvas boundary clamp) Stops player from walking off the left edge of the canvas entirely.
        Dim newX As Double = Math.Max(0, Canvas.GetLeft(imgPlayer) - 2)
        Canvas.SetLeft(imgPlayer, newX)
        rectPlayer = imageToRect(imgPlayer)
    End Sub

    Private Sub MoveRight()
        ' -DrManzo (IMPROVE 1 - canvas boundary clamp) Stops player from walking off the right edge. Subtracts the player sprite width so the whole image stays on screen.
        Dim newX As Double = Math.Min(mainCanvas.ActualWidth - imgPlayer.Width, Canvas.GetLeft(imgPlayer) + 2)
        Canvas.SetLeft(imgPlayer, newX)
        rectPlayer = imageToRect(imgPlayer)
    End Sub

    Private Sub MoveUp()
        ' -DrManzo (IMPROVE 1 - canvas boundary clamp) Stops player from walking off the top edge.
        Dim newY As Double = Math.Max(0, Canvas.GetTop(imgPlayer) - 2)
        Canvas.SetTop(imgPlayer, newY)
        rectPlayer = imageToRect(imgPlayer)
    End Sub

    Private Sub MoveDown()
        ' -DrManzo (IMPROVE 1 - canvas boundary clamp) Stops player from walking off the bottom edge. Subtracts sprite height so the full image stays visible.
        Dim newY As Double = Math.Min(mainCanvas.ActualHeight - imgPlayer.Height, Canvas.GetTop(imgPlayer) + 2)
        Canvas.SetTop(imgPlayer, newY)
        rectPlayer = imageToRect(imgPlayer)
    End Sub


    '--------------------------------------------------------------------------------------------------------------
    'Enemy Logic
    '--------------------------------------------------------------------------------------------------------------

    Private Sub HandleEnemyDefeat(enemy As Enemy)
        If enemy.LootDrop <> "" Then
            player.PickUpItem(enemy.LootDrop)
            AddToLog("You found: " & enemy.LootDrop)
            UpdateInventoryDisplay()
        End If
        btnAttack.Visibility = Visibility.Collapsed

        UpdateRoomDisplay()
        UpdateHealthBars()
        AddToLog("The room is now clear.")
    End Sub

    'Enemy Movement

    'Enemy Collision

End Class