Public Class Enemy
    'These are properties that can be declared when creating an instance of the Enemy class in the main window.
    Public Property Name As String
    Public Property Health As Integer
    Public Property MaxHealth As Integer
    Public Property AttackPower As Integer
    Public Property Description As String
    Public Property LootDrop As String


    'this is the constructor for the Enemy class, which initializes the properties when a new Enemy object is created.
    Public Sub New(eName As String, hp As Integer, atk As Integer)
        Name = eName
        Health = hp
        MaxHealth = hp
        AttackPower = atk
    End Sub

    Public Function IsAlive() As Boolean
        Return Health > 0
    End Function

    Public Function AttackPlayer(player As Player) As Integer
        Dim damage As Integer = AttackPower
        player.Health -= damage
        Return damage
    End Function
End Class