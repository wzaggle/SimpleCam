Imports System.Configuration
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.ComponentModel
Imports System.IO
Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Windows.Threading


Class MainWindow
    Public Rename_State = False
    Public SavePreset_State = False
    Public Data_Loaded = False
    Public Port = 1
    Public OBS = " "
    Public Cam1IPAddress = ""
    Public Cam2IPAddress = ""
    Public Cam1Speed As Int16 = 5
    Public Cam2Speed As Int16 = 5
    Public Cam1Zoom As Int16 = 0
    Public Cam2Zoom As Int16 = 0
    Public Preset1 As Byte
    Public Preset2 As Byte
    Private cpTimer As DispatcherTimer
    Private P1Timer As DispatcherTimer
    Private P2Timer As DispatcherTimer
    Public Sub New()
        cpTimer = New DispatcherTimer
        cpTimer.Interval = TimeSpan.FromMilliseconds(2000)
        AddHandler cpTimer.Tick, AddressOf CheckPresets
        cpTimer.Start()
        P1Timer = New DispatcherTimer
        P1Timer.Interval = TimeSpan.FromMilliseconds(6000)
        AddHandler P1Timer.Tick, AddressOf DelayPreset1
        P2Timer = New DispatcherTimer
        P2Timer.Interval = TimeSpan.FromMilliseconds(6000)
        AddHandler P2Timer.Tick, AddressOf DelayPreset2
    End Sub

    Private Sub Check_For_Cam1Preset()
        Dim FILE_NAME As String = Path.GetTempPath + "Preset1.dat"
        Dim objReader As System.IO.StreamReader
        Try
            objReader = New System.IO.StreamReader(FILE_NAME)
        Catch ex As Exception
            Return
        End Try
        Preset1 = CInt(objReader.Read() - 48)
        Dim delay = CInt(objReader.Read() - 48)
        objReader.Close()
        System.IO.File.Delete(FILE_NAME)
        If delay > 0 Then
            P1Timer.Interval = TimeSpan.FromMilliseconds(delay * 1000)
            P1Timer.Start()
            cpTimer.Stop()
        Else
            Do_Preset(Preset1, 2)
        End If
        Return
    End Sub
    Private Sub DelayPreset1()
        P1Timer.Stop()
        Do_Preset(Preset1, 2)
        cpTimer.Start()
        Return
    End Sub
    Private Sub Check_For_Cam2Preset()
        Dim FILE_NAME As String = Path.GetTempPath + "Preset2.dat"
        Dim objReader As System.IO.StreamReader
        Try
            objReader = New System.IO.StreamReader(FILE_NAME)
        Catch ex As Exception
            Return
        End Try
        Preset1 = CInt(objReader.Read() - 48)
        Dim delay = CInt(objReader.Read() - 48)
        objReader.Close()
        System.IO.File.Delete(FILE_NAME)
        If delay > 0 Then
            P2Timer.Interval = TimeSpan.FromMilliseconds(delay * 1000)
            P2Timer.Start()
            cpTimer.Stop()
        Else
            Do_Preset(Preset2, 2)
        End If
        Return
    End Sub
    Private Sub DelayPreset2()
        P2Timer.Stop()
        Do_Preset(Preset2, 2)
        cpTimer.Start()
        Return
    End Sub
    Private Sub CheckPresets()
        Check_For_Cam1Preset()
        Check_For_Cam2Preset()
        Return

    End Sub
    Private Sub Save_Button_Click(sender As Object, e As RoutedEventArgs) Handles Save_Button.Click
        If (SavePreset_State = False) Then
            SavePreset_State = True
            Save_Button.Content = "Select Preset"
        Else
            SavePreset_State = False
            Save_Button.Content = "Save Preset"
        End If

    End Sub
    Private Sub Rename_Button_Click(sender As Object, e As RoutedEventArgs) Handles Rename_Button.Click

        If (Rename_State = False) Then
            Rename_State = True
            Rename_Button.Content = "Select Preset"
        Else
            Rename_State = False
            Rename_Button.Content = "Rename"
        End If
    End Sub
    Private Sub GetZoom1()
        Dim outStream As Byte() = {&H81, &H9, &H4, &H47, &HFF}
        Dim clientSocket As New System.Net.Sockets.TcpClient()
        Dim numberOfBytesRead As Int16
        Try
            clientSocket.Connect(Cam1IPAddress, 1259)
        Catch ex As Exception
            Return
        End Try
        Dim serverStream1 As NetworkStream = clientSocket.GetStream()
        serverStream1.Write(outStream, 0, outStream.Length)
        serverStream1.Flush()
        Dim myReadBuffer(12) As Byte
        serverStream1.ReadTimeout = 5000
        Try
            numberOfBytesRead = serverStream1.Read(myReadBuffer, 0, myReadBuffer.Length)
        Catch ex As Exception
            Return
        End Try
        serverStream1.Flush()
        If numberOfBytesRead = 7 Then
            Zoom1.Value = ((myReadBuffer(2) * 10) + (myReadBuffer(3)))
            If (myReadBuffer(4) > 9) Then Zoom1.Value += 1
        End If
        Return
    End Sub
    Private Sub GetZoom2()
        Dim outStream As Byte() = {&H81, &H9, &H4, &H47, &HFF}
        Dim clientSocket As New System.Net.Sockets.TcpClient()
        Dim numberOfBytesRead As Int16
        Try
            clientSocket.Connect(Cam2IPAddress, 1259)
        Catch ex As Exception
            clientSocket.Close()
            Return
        End Try
        Dim serverStream1 As NetworkStream = clientSocket.GetStream()
        serverStream1.Write(outStream, 0, outStream.Length)
        serverStream1.Flush()
        Dim myReadBuffer(12) As Byte
        serverStream1.ReadTimeout = 5000
        Try
            numberOfBytesRead = serverStream1.Read(myReadBuffer, 0, myReadBuffer.Length)
        Catch ex As Exception
            clientSocket.Close()
            Return
        End Try
        serverStream1.Flush()
        If numberOfBytesRead = 7 Then
            Zoom2.Value = ((myReadBuffer(2) * 10) + (myReadBuffer(3)))
            If (myReadBuffer(4) > 9) Then Zoom2.Value += 1
        End If
        clientSocket.Close()
        Return
    End Sub
    Private Sub Do_Preset(Number As Byte, SetRecall As Byte)
        Dim myReadBuffer(12) As Byte
        Dim numberOfBytesRead As Int16
        Dim clientSocket As New System.Net.Sockets.TcpClient()
        Dim BadIP As String
        Try
            If Number < 11 Then
                BadIP = Cam1IPAddress
                clientSocket.Connect(Cam1IPAddress, 1259)
            Else
                BadIP = Cam2IPAddress
                clientSocket.Connect(Cam2IPAddress, 1259)
                Number -= 10
            End If
        Catch ex As Exception
            MsgBox("Not able to connect to " + BadIP)
            clientSocket.Dispose()
            Return
        End Try
        Dim serverStream1 As NetworkStream = clientSocket.GetStream() 'set new preset in camera or recall
        Dim outStream1 As Byte() = {&H81, &H1, &H4, &H3F, SetRecall, Number - 1, &HFF}
        serverStream1.Write(outStream1, 0, outStream1.Length)
        serverStream1.Flush()
        serverStream1.ReadTimeout = 10000
        Try
            numberOfBytesRead = serverStream1.Read(myReadBuffer, 0, 3)
        Catch ex As Exception
            clientSocket.Close()
            Return
        End Try
        Try
            numberOfBytesRead = serverStream1.Read(myReadBuffer, 0, 3)
        Catch ex As Exception
            clientSocket.Close()
            Return
        End Try
        serverStream1.Flush()
        If SetRecall = 1 Then 'set move speed to fast if set
            Dim outStream2 As Byte() = {&H81, &H1, &H7E, &H1, &HB, Number - 1, &H18, &HFF}
            serverStream1.Write(outStream2, 0, outStream2.Length)
            serverStream1.Flush()
            Try
                numberOfBytesRead = serverStream1.Read(myReadBuffer, 0, myReadBuffer.Length)
            Catch ex As Exception
                clientSocket.Close()
                Return
            End Try
            serverStream1.Flush()
        End If
        If SetRecall = 2 Then 'set callback timer to reset zoom level after preset recall
            If Number < 11 Then
                GetZoom1()
            Else
                GetZoom2()
            End If
        End If
        clientSocket.Close()
        Return
    End Sub


    Private Sub Preset_Click(sender As Object, e As RoutedEventArgs) Handles Button1A.Click, Button6A.Click, Button2A.Click, Button7A.Click, Button3A.Click, Button8A.Click, Button4A.Click, Button9A.Click, Button5A.Click, Button10A.Click, Button1B.Click, Button6B.Click, Button2B.Click, Button7B.Click, Button3B.Click, Button8B.Click, Button4B.Click, Button9B.Click, Button10B.Click, Button5B.Click

        Dim FILE_NAME As String = Path.GetTempPath + "Presets.dat"
        Dim Tries = 100
        Dim objWriter As System.IO.StreamWriter
        If Rename_State Then
            Do While Tries > 0
                Try
                    objWriter = New System.IO.StreamWriter(FILE_NAME)
                Catch ex As Exception
                    Tries -= 1
                    Threading.Thread.Sleep(500)
                    Continue Do
                End Try
                If Tries > 0 Then
                    Dim presets As String
                    Dim indexLabel As String
                    Dim folder As String = System.Windows.Forms.Application.StartupPath
                    Dim cAppConfig As System.Configuration.Configuration = System.Configuration.ConfigurationManager.OpenExeConfiguration(folder & "\SimpleCam.exe")
                    Dim asSettings As AppSettingsSection = cAppConfig.AppSettings

                    indexLabel = "Button" + CType(sender, Button).Uid

                    Dim NewValue As String = ""
                    NewValue = InputBox("Enter a new Preset Name: ", "Preset Name", CType(sender, Button).Content)
                    If NewValue <> "" Then
                        If NewValue.Length > 13 Then NewValue = NewValue.Substring(0, 13)
                        asSettings.Settings.Item(indexLabel).Value = NewValue
                        cAppConfig.Save(ConfigurationSaveMode.Modified)
                        CType(sender, Button).Content = NewValue
                    End If
                    Rename_State = False
                    Rename_Button.Content = "Rename"
                    presets = Button1A.Content + ", "
                    presets = presets + Button2A.Content + ", "
                    presets = presets + Button3A.Content + ", "
                    presets = presets + Button4A.Content + ", "
                    presets = presets + Button5A.Content + ", "
                    presets = presets + Button6A.Content + ", "
                    presets = presets + Button7A.Content + ", "
                    presets = presets + Button8A.Content + ", "
                    presets = presets + Button9A.Content + ", "
                    presets = presets + Button10A.Content + ", "
                    presets = presets + Button1B.Content + ", "
                    presets = presets + Button2B.Content + ", "
                    presets = presets + Button3B.Content + ", "
                    presets = presets + Button4B.Content + ", "
                    presets = presets + Button5B.Content + ", "
                    presets = presets + Button6B.Content + ", "
                    presets = presets + Button7B.Content + ", "
                    presets = presets + Button8B.Content + ", "
                    presets = presets + Button9B.Content + ", "
                    presets = presets + Button10B.Content

                    objWriter.Write(presets)
                    objWriter.Close()
                    Do_Preset(CInt(CType(sender, Button).Uid), 1)
                    Return
                Else
                    objWriter.Close()
                    Return
                End If
            Loop

        ElseIf (SavePreset_State) Then
            SavePreset_State = False
            Save_Button.Content = "Set Preset"
            Do_Preset(CInt(CType(sender, Button).Uid), 0)
            Do_Preset(CInt(CType(sender, Button).Uid), 1)
        Else
            Do_Preset(CInt(CType(sender, Button).Uid), 2)
        End If
    End Sub

    Private Sub UserForm_Initialize()
        Dim folder As String = System.Windows.Forms.Application.StartupPath
        Dim cAppConfig As System.Configuration.Configuration = System.Configuration.ConfigurationManager.OpenExeConfiguration(folder & "\SimpleCam.exe")
        Dim appSettings = System.Configuration.ConfigurationManager.AppSettings
        Dim FILE_NAME As String = Path.GetTempPath + "Presets.dat"
        Button1A.Content = appSettings("Button1")
        Button2A.Content = appSettings("Button2")
        Button3A.Content = appSettings("Button3")
        Button4A.Content = appSettings("Button4")
        Button5A.Content = appSettings("Button5")
        Button6A.Content = appSettings("Button6")
        Button7A.Content = appSettings("Button7")
        Button8A.Content = appSettings("Button8")
        Button9A.Content = appSettings("Button9")
        Button10A.Content = appSettings("Button10")
        Button1B.Content = appSettings("Button11")
        Button2B.Content = appSettings("Button12")
        Button3B.Content = appSettings("Button13")
        Button4B.Content = appSettings("Button14")
        Button5B.Content = appSettings("Button15")
        Button6B.Content = appSettings("Button16")
        Button7B.Content = appSettings("Button17")
        Button8B.Content = appSettings("Button18")
        Button9B.Content = appSettings("Button19")
        Button10B.Content = appSettings("Button20")
        Cam1IPAddress = appSettings("Cam1IP")
        Cam2IPAddress = appSettings("Cam2IP")
        Speed1.Value = CInt(appSettings("Cam1Speed"))
        Speed2.Value = CInt(appSettings("Cam2Speed"))
        Backlight1.IsChecked = (appSettings("Backlighting1") = "True")
        Backlight2.IsChecked = (appSettings("Backlighting2") = "True")
        Compensate1.IsChecked = (appSettings("Compensation1") = "True")
        Compensate2.IsChecked = (appSettings("Compensation2") = "True")


        Dim presets As String
        presets = Button1A.Content + ", "
        presets = presets + Button2A.Content + ", "
        presets = presets + Button3A.Content + ", "
        presets = presets + Button4A.Content + ", "
        presets = presets + Button5A.Content + ", "
        presets = presets + Button6A.Content + ", "
        presets = presets + Button7A.Content + ", "
        presets = presets + Button8A.Content + ", "
        presets = presets + Button9A.Content + ", "
        presets = presets + Button10A.Content + ", "
        presets = presets + Button1B.Content + ", "
        presets = presets + Button2B.Content + ", "
        presets = presets + Button3B.Content + ", "
        presets = presets + Button4B.Content + ", "
        presets = presets + Button5B.Content + ", "
        presets = presets + Button6B.Content + ", "
        presets = presets + Button7B.Content + ", "
        presets = presets + Button8B.Content + ", "
        presets = presets + Button9B.Content + ", "
        presets = presets + Button10B.Content
        Dim objWriter As System.IO.StreamWriter
        objWriter = New System.IO.StreamWriter(FILE_NAME)
        objWriter.Write(presets)
        objWriter.Close()
        cpTimer.IsEnabled = True
    End Sub

    Private Sub MainWindow_Activated(sender As Object, e As EventArgs) Handles Me.Activated
        If (Not Data_Loaded) Then
            UserForm_Initialize()
            Data_Loaded = True
        End If
    End Sub

    Private Sub Send_Packet(IP As String, Packet As Byte())
        Dim clientSocket As New System.Net.Sockets.TcpClient()
        Dim numberOfBytesRead As Int16
        Dim myReadBuffer(12) As Byte
        Try
            clientSocket.Connect(IP, 1259)
        Catch ex As Exception
            clientSocket.Close()
            Return
        End Try

        Dim serverStream1 As NetworkStream = clientSocket.GetStream()
        serverStream1.Write(Packet, 0, Packet.Length)
        serverStream1.Flush()
        serverStream1.ReadTimeout = 5000
        Try
            numberOfBytesRead = serverStream1.Read(myReadBuffer, 0, 3)
        Catch ex As Exception
            serverStream1.Close()
            clientSocket.Close()
            Return
        End Try
        Try
            numberOfBytesRead = serverStream1.Read(myReadBuffer, 0, 3)
        Catch ex As Exception
            serverStream1.Close()
            clientSocket.Close()
            Return
        End Try
        serverStream1.Flush()
        serverStream1.Close()
        clientSocket.Close()
        Return
    End Sub
    Private Function NibbleNum(X As Byte) As Byte()
        Dim Nibbles(4) As Byte
        Nibbles(0) = (X \ 10)
        Nibbles(1) = X - (Nibbles(0) * 10)
        Nibbles(2) = 0
        Nibbles(3) = 0
        Return Nibbles
    End Function

    Private Sub Do_Camera1_IP(sender As Object, e As RoutedEventArgs) Handles CIP1.Click
        Dim folder As String = System.Windows.Forms.Application.StartupPath
        Dim cAppConfig As System.Configuration.Configuration = System.Configuration.ConfigurationManager.OpenExeConfiguration(folder & "\SimpleCam.exe")
        Dim asSettings As AppSettingsSection = cAppConfig.AppSettings
        Dim NewValue As String = ""
        NewValue = InputBox("Enter IP Address for Camera1: ", "Camera IP Address", Cam1IPAddress)
        If NewValue <> "" Then
            asSettings.Settings.Item("Cam1IP").Value = NewValue
            cAppConfig.Save(ConfigurationSaveMode.Modified)
            Cam1IPAddress = NewValue
        End If

    End Sub

    Private Sub Do_Camera2_IP(sender As Object, e As RoutedEventArgs) Handles CIP2.Click
        Dim folder As String = System.Windows.Forms.Application.StartupPath
        Dim cAppConfig As System.Configuration.Configuration = System.Configuration.ConfigurationManager.OpenExeConfiguration(folder & "\SimpleCam.exe")
        Dim asSettings As AppSettingsSection = cAppConfig.AppSettings
        Dim NewValue As String = ""
        NewValue = InputBox("Enter IP Address for Camera1: ", "Camera IP Address", Cam2IPAddress)
        If NewValue <> "" Then
            asSettings.Settings.Item("Cam2IP").Value = NewValue
            cAppConfig.Save(ConfigurationSaveMode.Modified)
            Cam2IPAddress = NewValue
        End If
    End Sub

    Private Sub Do_Button1_Release(sender As Object, e As MouseButtonEventArgs) Handles C1UP.PreviewMouseLeftButtonUp, C1UR.PreviewMouseLeftButtonUp, C1RT.PreviewMouseLeftButtonUp, C1DR.PreviewMouseLeftButtonUp, C1DN.PreviewMouseLeftButtonUp, C1DL.PreviewMouseLeftButtonUp, C1LT.PreviewMouseLeftButtonUp, C1UL.PreviewMouseLeftButtonUp
        Send_Packet(Cam1IPAddress, {&H81, &H1, &H6, &H1, Speed1.Value, Speed1.Value, 3, 3, &HFF})
    End Sub

    Private Sub Do_Button2_Release(sender As Object, e As MouseButtonEventArgs) Handles C2UP.PreviewMouseLeftButtonUp, C2UR.PreviewMouseLeftButtonUp, C2RT.PreviewMouseLeftButtonUp, C2DR.PreviewMouseLeftButtonUp, C2DN.PreviewMouseLeftButtonUp, C2DL.PreviewMouseLeftButtonUp, C2LT.PreviewMouseLeftButtonUp, C2UL.PreviewMouseLeftButtonUp
        Send_Packet(Cam2IPAddress, {&H81, &H1, &H6, &H1, Speed2.Value, Speed2.Value, 3, 3, &HFF})
    End Sub

    Private Sub Do_Button1_Down(sender As Object, e As MouseButtonEventArgs) Handles C1UP.PreviewMouseLeftButtonDown, C1UR.PreviewMouseLeftButtonDown, C1RT.PreviewMouseLeftButtonDown, C1DR.PreviewMouseLeftButtonDown, C1DN.PreviewMouseLeftButtonDown, C1DL.PreviewMouseLeftButtonDown, C1LT.PreviewMouseLeftButtonDown, C1UL.PreviewMouseLeftButtonDown
        Dim Direction0() As Byte = {3, 2, 2, 3, 3, 1, 1, 1, 3}
        Dim Direction1() As Byte = {1, 1, 3, 3, 2, 2, 3, 1, 3}
        Dim outStream As Byte() = {&H81, &H1, &H6, &H1, Speed1.Value, Speed1.Value, Direction0(CInt(CType(sender, Button).Uid) - 1), Direction1(CInt(CType(sender, Button).Uid) - 1), &HFF}
        Send_Packet(Cam1IPAddress, outStream)
    End Sub

    Private Sub Do_Button2_Down(sender As Object, e As MouseButtonEventArgs) Handles C2UP.PreviewMouseLeftButtonDown, C2UR.PreviewMouseLeftButtonDown, C2RT.PreviewMouseLeftButtonDown, C2DR.PreviewMouseLeftButtonDown, C2DN.PreviewMouseLeftButtonDown, C2DL.PreviewMouseLeftButtonDown, C2LT.PreviewMouseLeftButtonDown, C2UL.PreviewMouseLeftButtonDown
        Dim Direction0() As Byte = {3, 2, 2, 3, 3, 1, 1, 1, 3}
        Dim Direction1() As Byte = {1, 1, 3, 3, 2, 2, 3, 1, 3}
        Dim outStream As Byte() = {&H81, &H1, &H6, &H1, Speed2.Value, Speed2.Value, Direction0(CInt(CType(sender, Button).Uid) - 1), Direction1(CInt(CType(sender, Button).Uid) - 1), &HFF}
        Send_Packet(Cam2IPAddress, outStream)
    End Sub

    Private Sub ZoomValue1(sender As Object, e As MouseButtonEventArgs) Handles Zoom1.PreviewMouseUp
        Dim zoomFactor As Byte() = NibbleNum(Zoom1.Value)
        Dim outStream As Byte() = {&H81, &H1, &H4, &H47, zoomFactor(0), zoomFactor(1), 1, 0, &HFF}
        Send_Packet(Cam1IPAddress, outStream)
    End Sub

    Private Sub ZoomValue2(sender As Object, e As MouseButtonEventArgs) Handles Zoom2.PreviewMouseUp
        Dim zoomFactor As Byte() = NibbleNum(Zoom2.Value)
        Dim outStream As Byte() = {&H81, &H1, &H4, &H47, zoomFactor(0), zoomFactor(1), 1, 0, &HFF}
        Send_Packet(Cam2IPAddress, outStream)
    End Sub

    Private Sub Do_UpdateConfig(sender As Object, e As MouseButtonEventArgs) Handles Speed1.PreviewMouseUp, Speed2.PreviewMouseUp
        Dim folder As String = System.Windows.Forms.Application.StartupPath
        Dim cAppConfig As System.Configuration.Configuration = System.Configuration.ConfigurationManager.OpenExeConfiguration(folder & "\SimpleCam.exe")
        Dim asSettings As AppSettingsSection = cAppConfig.AppSettings
        asSettings.Settings.Item("Cam1Speed").Value = Speed1.Value.ToString
        asSettings.Settings.Item("Cam2Speed").Value = Speed2.Value.ToString
        cAppConfig.Save(ConfigurationSaveMode.Modified)
    End Sub

    Private Sub DoAutofocus2(sender As Object, e As RoutedEventArgs) Handles AutoFocus2.Checked, AutoFocus2.Unchecked
        Dim outStream As Byte() = {&H81, &H1, &H4, &H38, &H2, &HFF}
        If AutoFocus2.IsChecked Then
            outStream(4) = &H3
        End If
        Send_Packet(Cam2IPAddress, outStream)
    End Sub

    Private Sub DoAutofocus1(sender As Object, e As RoutedEventArgs) Handles AutoFocus1.Checked, AutoFocus1.Unchecked
        Dim outStream As Byte() = {&H81, &H1, &H4, &H38, &H2, &HFF}
        If AutoFocus1.IsChecked Then
            outStream(4) = &H3
        End If
        Send_Packet(Cam1IPAddress, outStream)
    End Sub

    Private Sub DoComp1(sender As Object, e As RoutedEventArgs) Handles Compensate2.Checked, Compensate2.Unchecked
        Dim folder As String = System.Windows.Forms.Application.StartupPath
        Dim cAppConfig As System.Configuration.Configuration = System.Configuration.ConfigurationManager.OpenExeConfiguration(folder & "\SimpleCam.exe")
        Dim asSettings As AppSettingsSection = cAppConfig.AppSettings
        Dim outStream As Byte() = {&H81, &H1, &H4, &H33, &H3, &HFF}
        Dim BLState As String = "False"
        If Compensate1.IsChecked Then
            BLState = "True"
            outStream(4) = &H2
        End If
        Send_Packet(Cam1IPAddress, outStream)
        asSettings.Settings.Item("Compensation1").Value = BLState
        cAppConfig.Save(ConfigurationSaveMode.Modified)
    End Sub

    Private Sub DoComp2(sender As Object, e As RoutedEventArgs) Handles Compensate1.Checked, Compensate1.Unchecked
        Dim folder As String = System.Windows.Forms.Application.StartupPath
        Dim cAppConfig As System.Configuration.Configuration = System.Configuration.ConfigurationManager.OpenExeConfiguration(folder & "\SimpleCam.exe")
        Dim asSettings As AppSettingsSection = cAppConfig.AppSettings
        Dim outStream As Byte() = {&H81, &H1, &H4, &H33, &H3, &HFF}
        Dim BLState As String = "False"
        If Compensate2.IsChecked Then
            BLState = "True"
            outStream(4) = &H2
        End If
        Send_Packet(Cam2IPAddress, outStream)
        asSettings.Settings.Item("Compensation2").Value = BLState
        cAppConfig.Save(ConfigurationSaveMode.Modified)
    End Sub

    Private Sub DoFocus2Plus(sender As Object, e As MouseButtonEventArgs) Handles Focus1P.PreviewMouseDown, Focus2P.PreviewMouseDown
        Dim outStream As Byte() = {&H81, &H1, &H4, &H8, &H3, &HFF}
        If CType(sender, Button).Uid = "1" Then
            Send_Packet(Cam1IPAddress, outStream)
        Else
            Send_Packet(Cam2IPAddress, outStream)
        End If
    End Sub

    Private Sub DoFocus2Minus(sender As Object, e As MouseButtonEventArgs) Handles Focus1M.PreviewMouseDown, Focus2M.PreviewMouseDown
        Dim outStream As Byte() = {&H81, &H1, &H4, &H8, &H2, &HFF}
        If CType(sender, Button).Uid = "1" Then
            Send_Packet(Cam1IPAddress, outStream)
        Else
            Send_Packet(Cam2IPAddress, outStream)
        End If
    End Sub

    Private Sub DoFocus1Plus(sender As Object, e As MouseButtonEventArgs) Handles Focus1P.PreviewMouseUp, Focus2P.PreviewMouseUp, Focus1M.PreviewMouseUp, Focus2M.PreviewMouseUp
        Dim outStream As Byte() = {&H81, &H1, &H4, &H7, &H0, &HFF}
        Send_Packet(Cam1IPAddress, outStream)
    End Sub


    Private Sub DoC1WB(sender As Object, e As RoutedEventArgs) Handles C1WB.Click
        Dim outStream As Byte() = {&H81, &H1, &H4, &H35, &H3, &HFF}
        Send_Packet(Cam1IPAddress, outStream)
    End Sub

    Private Sub DOC2WB(sender As Object, e As RoutedEventArgs) Handles C2WB.Click
        Dim outStream As Byte() = {&H81, &H1, &H4, &H35, &H3, &HFF}
        Send_Packet(Cam2IPAddress, outStream)
    End Sub

    Private Sub DoBacklight1(sender As Object, e As RoutedEventArgs) Handles Backlight1.Checked, Backlight1.Unchecked
        Dim folder As String = System.Windows.Forms.Application.StartupPath
        Dim cAppConfig As System.Configuration.Configuration = System.Configuration.ConfigurationManager.OpenExeConfiguration(folder & "\SimpleCam.exe")
        Dim asSettings As AppSettingsSection = cAppConfig.AppSettings
        Dim outStream As Byte() = {&H81, &H1, &H4, &H33, &H3, &HFF}
        Dim BLState As String = "False"
        If Backlight1.IsChecked Then
            BLState = "True"
            outStream(4) = &H2
        End If
        Send_Packet(Cam1IPAddress, outStream)
        asSettings.Settings.Item("Backlighting1").Value = BLState
        cAppConfig.Save(ConfigurationSaveMode.Modified)
    End Sub

    Private Sub DoBacklight2(sender As Object, e As RoutedEventArgs) Handles Backlight2.Checked, Backlight2.Unchecked
        Dim folder As String = System.Windows.Forms.Application.StartupPath
        Dim cAppConfig As System.Configuration.Configuration = System.Configuration.ConfigurationManager.OpenExeConfiguration(folder & "\SimpleCam.exe")
        Dim asSettings As AppSettingsSection = cAppConfig.AppSettings
        Dim outStream As Byte() = {&H81, &H1, &H4, &H33, &H3, &HFF}
        Dim BLState As String = "False"
        If Backlight2.IsChecked Then
            BLState = "True"
            outStream(4) = &H2
        End If
        Send_Packet(Cam2IPAddress, outStream)
        asSettings.Settings.Item("Backlighting2").Value = BLState
        cAppConfig.Save(ConfigurationSaveMode.Modified)
    End Sub

    Private Sub DoHome2(sender As Object, e As MouseButtonEventArgs) Handles C2HM.MouseDoubleClick
        Dim outStream As Byte() = {&H81, &H1, &H6, &H4, &HFF}
        Send_Packet(Cam2IPAddress, outStream)
    End Sub

    Private Sub DoHome1(sender As Object, e As MouseButtonEventArgs) Handles C1HM.MouseDoubleClick
        Dim outStream As Byte() = {&H81, &H1, &H6, &H4, &HFF}
        Send_Packet(Cam1IPAddress, outStream)
    End Sub

End Class
