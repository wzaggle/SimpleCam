Imports System.Configuration
Imports System.Web.Services
Imports System.Web.Services.Protocols
Imports System.ComponentModel
Imports System.IO
Imports System.IO.Ports
Imports System
Imports System.Net
Imports System.Net.Sockets
Imports System.Windows.Threading
Class MainWindow
    Private Rename_State = False
    Private SavePreset_State = False
    Private Preset As Byte() = {0, 0, 0, 0, 0}
    Private Direction0() As Byte = {3, 2, 2, 3, 3, 1, 1, 1, 3}
    Private Direction1() As Byte = {1, 1, 3, 3, 2, 2, 3, 1, 3}
    Private efTimer As DispatcherTimer
    Private cpTimer As DispatcherTimer
    Private PTimer As DispatcherTimer()
    Private disabled As Boolean() = {True, True, True, True, True}
    Private Cam1 As Byte = 1
    Private Cam2 As Byte = 2
    Private CamOn() As Boolean = {False, False, False, False, False}
    Private CamOnOffButton(2) As Button
    Private CamSelect As Boolean = False
    Private SettingsActive As Boolean = False
    Private EditButton As Button
    Private SingleCam As Boolean = False
    Private StaySingle As Boolean = False
    Private Brush1 = New SolidColorBrush(Color.FromArgb(255, &H3C, &H96, &HC6))
    Private Brush2 = New SolidColorBrush(Color.FromArgb(255, &H6C, &HC9, &H96))
    Private Brush3 = New SolidColorBrush(Color.FromArgb(255, &HC9, &H3C, &H3C)) 'off
    Private Brush4 = New SolidColorBrush(Color.FromArgb(255, &H3C, &HC9, &H3C)) 'on
    Private TempPath As String
    Private Shuttle1 As Boolean = False
    Private Shuttle1Direction As Byte
    Private Shuttle2Direction As Byte
    Private Shuttle2 As Boolean = False
    Public Sub New()
        Dim Cam As Byte
        InitializeComponent()
        cpTimer = New DispatcherTimer
        cpTimer.Interval = TimeSpan.FromMilliseconds(2000)
        AddHandler cpTimer.Tick, AddressOf CheckPresets
        cpTimer.IsEnabled = True
        cpTimer.Start()
        efTimer = New DispatcherTimer(10)
        efTimer.Interval = TimeSpan.FromMilliseconds(80)
        AddHandler efTimer.Tick, AddressOf EffectReset
        Dim i = 0
        PTimer = Enumerable.
        Range(1, 5).Select(Function(item) New DispatcherTimer).ToArray()
        For Each timer As DispatcherTimer In PTimer
            timer.Tag = i
            AddHandler timer.Tick, Sub(sender As Object, args As EventArgs)
                                       Dim t As DispatcherTimer = CType(sender, DispatcherTimer)
                                       t.Stop()
                                       Do_Preset(t.Tag, Preset(t.Tag), 2, True)
                                       cpTimer.Start()
                                   End Sub
            i += 1
        Next
        TempPath = Path.GetTempPath
        CamOnOffButton(0) = CamOnOff1
        CamOnOffButton(1) = CamOnOff2
        For Cam = 1 To 5
            Send_Packet(Cam, {&H80, &H1, &H4, &H0, &H2, &HFF})
            GetCamOnOff(Cam)
        Next Cam
        SetCams()
        UpdatePresets()
    End Sub
    Private Sub StartEffect()
        Dim Brush = New SolidColorBrush(Color.FromArgb(250, 250, 250, 10))
        ButtonText.Background = Brush
        Grid.SetRow(ButtonText, Grid.GetRow(EditButton))
        Grid.SetColumn(ButtonText, Grid.GetColumn(EditButton))
        ButtonText.Visibility = Visibility.Visible
        ButtonText.Text = EditButton.Content
        ButtonText.IsEnabled = False
        ButtonText.Focus()
        efTimer.Start()
    End Sub
    Private Sub EffectReset()
        Dim CurrentBrush As SolidColorBrush
        CurrentBrush = ButtonText.Background
        Dim NewBrush = New SolidColorBrush(Color.FromArgb(255, CurrentBrush.Color.R - 10, CurrentBrush.Color.G - 10, CurrentBrush.Color.B + 20))
        ButtonText.Background = NewBrush
        ButtonText.Focus()
        If NewBrush.Color.B = 170 Then
            Dim request = New TraversalRequest(FocusNavigationDirection.First)
            request.Wrapped = True
            ButtonText.MoveFocus(request)
            ButtonText.IsEnabled = False
            ButtonText.Visibility = Visibility.Hidden
            efTimer.Stop()
        End If
    End Sub

    Private Sub CheckPresets()
        '     If disable1.IsChecked Then Return
        For i = 1 To 5
            Dim FILE_NAME As String = TempPath + "SCPreset" + i.ToString + ".dat"
            Dim objReader As System.IO.StreamReader
            Try
                objReader = New System.IO.StreamReader(FILE_NAME)
            Catch ex As System.IO.IOException
                Continue For
            End Try
            Dim line = objReader.ReadLine()

            Dim values As String() = line.Split(New Char() {","c})
            Preset(i) = CInt(values(0))
            Dim delay = CInt(values(1))
            objReader.Close()
            System.IO.File.Delete(FILE_NAME)
            If delay > 0 Then
                cpTimer.Stop()
                PTimer(i).Interval = TimeSpan.FromMilliseconds(delay * 1000)
                PTimer(i).Start()
            Else
                Do_Preset(i, Preset(i), 2, True)
            End If
        Next
        Return
    End Sub
    Private Sub Save_Button_Click(sender As Object, e As RoutedEventArgs) Handles Save_Button.Click
        If (SavePreset_State = False) Then
            SavePreset_State = True
            Save_Button.Content = "Select Preset"
            Rename_State = False
            Rename_Button.Content = "Rename"
            Rename_Button2.Content = "Rename"
        Else
            SavePreset_State = False
            Save_Button.Content = "Set"
            Save_Button2.Content = "Set"
        End If
    End Sub
    Private Sub Rename_Button_Click(sender As Object, e As RoutedEventArgs) Handles Rename_Button.Click
        If (Rename_State = False) Then
            Rename_State = True
            Rename_Button.Content = "Select Preset"
            SavePreset_State = False
            Save_Button2.Content = "Set"
            Save_Button.Content = "Set"
        Else
            Rename_State = False
            Rename_Button.Content = "Rename"
            Rename_Button2.Content = "Rename"
        End If
    End Sub
    Private Sub Save_Button_Click2(sender As Object, e As RoutedEventArgs) Handles Save_Button2.Click
        If (SavePreset_State = False) Then
            SavePreset_State = True
            Save_Button2.Content = "Select Preset"
            Rename_State = False
            Rename_Button.Content = "Rename"
            Rename_Button2.Content = "Rename"
        Else
            SavePreset_State = False
            Save_Button2.Content = "Set"
            Save_Button.Content = "Set"
        End If
    End Sub
    Private Sub Rename_Button_Click2(sender As Object, e As RoutedEventArgs) Handles Rename_Button2.Click
        If (Rename_State = False) Then
            Rename_State = True
            Rename_Button2.Content = "Select Preset"
            SavePreset_State = False
            Save_Button2.Content = "Set"
            Save_Button.Content = "Set"
        Else
            Rename_State = False
            Rename_Button.Content = "Rename"
            Rename_Button2.Content = "Rename"
        End If
    End Sub
    Private Function SendCommand(Cam As Byte, Command As Byte(), IsCMD As Boolean) As Byte()
        Dim myReadBuffer() As Byte = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        If My.Settings.Disabled(Cam - 1) = "True" Then Return myReadBuffer
        Dim numberOfBytesRead As Int16 = 0
        Command(0) = &H80 + My.Settings.CamAdd(Cam - 1)
        If My.Settings.SerialIP(Cam - 1) = 0 Then 'do IP
            If My.Settings.CamIP(Cam - 1) = "X" Then Return myReadBuffer
            Dim clientSocket As New System.Net.Sockets.TcpClient()

                Try 'Send Command
                    clientSocket.Connect(My.Settings.CamIP(Cam - 1), My.Settings.CamPort(Cam - 1))
                Catch ex As System.IO.IOException
                Catch ex As Sockets.SocketException
                    My.Settings.Disabled(Cam - 1) = "True"
                    Return myReadBuffer
                End Try
                Dim serverStream1 As NetworkStream = clientSocket.GetStream()
                serverStream1.Write(Command, 0, Command.Length)
                serverStream1.Flush()
                serverStream1.ReadTimeout = 5000

                Try 'Receive ACK or Data
                    numberOfBytesRead = serverStream1.Read(myReadBuffer, 0, myReadBuffer.Length)
                Catch ex As System.IO.IOException
                Catch ex As Sockets.SocketException
                    My.Settings.Disabled(Cam - 1) = "True"
                    serverStream1.Close()
                    clientSocket.Close()
                    ReDim myReadBuffer(1)
                    Return myReadBuffer
                End Try
                If IsCMD And numberOfBytesRead < 4 Then 'Receive Completion
                    Try
                        numberOfBytesRead = serverStream1.Read(myReadBuffer, 0, myReadBuffer.Length)
                    Catch ex As System.IO.IOException
                    Catch ex As Sockets.SocketException
                        serverStream1.Close()
                        clientSocket.Close()
                        ReDim myReadBuffer(1)
                        Return myReadBuffer
                    End Try
                End If
                serverStream1.Flush()
                serverStream1.Close()
                clientSocket.Close()
                ReDim Preserve myReadBuffer(numberOfBytesRead - 1)
                Return myReadBuffer
            Else 'Do Serial
                settings.Content = "Com Port2"
            Dim ComPort As New SerialPort
            If OpenComPort(My.Settings.CamCom(Cam - 1), ComPort) Then
                ComPort.Write(Command, 0, Command.Length)
                ComPort.ReadTimeout = 5000
                Try
                    While numberOfBytesRead < 10
                        myReadBuffer(numberOfBytesRead) = ComPort.ReadByte()
                        numberOfBytesRead += 1
                        If myReadBuffer(numberOfBytesRead - 1) = 255 Then Exit While
                    End While
                Catch ex As System.IO.IOException
                Catch ex As System.TimeoutException
                    My.Settings.Disabled(Cam - 1) = "True"
                    ComPort.Close()
                    ReDim myReadBuffer(1)
                    Return myReadBuffer
                End Try
                If IsCMD And numberOfBytesRead < 4 Then 'Receive Completion
                    numberOfBytesRead = 0
                    Try
                        While numberOfBytesRead < 10
                            myReadBuffer(numberOfBytesRead) = ComPort.ReadByte()
                            numberOfBytesRead += 1
                            If myReadBuffer(numberOfBytesRead - 1) = 255 Then Exit While
                        End While
                    Catch ex As System.IO.IOException
                    Catch ex As System.TimeoutException
                        ComPort.Close()
                        ReDim myReadBuffer(1)
                        Return myReadBuffer
                    End Try
                End If
                ReDim Preserve myReadBuffer(numberOfBytesRead - 1)
                ComPort.Close()
                Return myReadBuffer
            End If

        End If
    End Function
    Private Sub GetCamOnOff(cam As Byte)
        Dim myReadBuffer As Byte() = SendCommand(cam, {&H80, &H9, &H4, &H0, &HFF}, False)
        If myReadBuffer.Length = 4 Then
            CamOn(cam - 1) = (myReadBuffer(2) = 2)
        End If
    End Sub

    Private Sub GetZoom(Cam As Byte)
        If My.Settings.Disabled(Cam - 1) = "True" Then Return
        If My.Settings.CamIP(Cam - 1) = "X" Then Return
        Dim myReadBuffer As Byte() = SendCommand(Cam, {&H81, &H9, &H4, &H47, &HFF}, False)
        If myReadBuffer.Length = 7 Then
            If (Cam = Cam1) Then
                Zoom1.Value = ((myReadBuffer(2) * 10) + (myReadBuffer(3)))
                If (myReadBuffer(4) > 9) Then Zoom1.Value += 1
            Else
                Zoom2.Value = ((myReadBuffer(2) * 10) + (myReadBuffer(3)))
                If (myReadBuffer(4) > 9) Then Zoom2.Value += 1
            End If
        End If
    End Sub
    Function OpenComPort(Name As String, ComPort As SerialPort) As Boolean
        Try
            ' Get the selected COM port’s name
            ' from the combo box.
            If Not ComPort.IsOpen Then
                ComPort.PortName = Name
                ComPort.BaudRate = 9600
                ComPort.Parity = Parity.None
                ComPort.DataBits = 8
                ComPort.StopBits = StopBits.One
                ComPort.Handshake = Handshake.None
                ComPort.ReadTimeout = 3000
                ComPort.WriteTimeout = 5000
                ' Open the port.
                ComPort.Open()
            End If
        Catch ex As InvalidOperationException
            MessageBox.Show(ex.Message)
            Return False
        Catch ex As UnauthorizedAccessException
            MessageBox.Show(ex.Message)
            Return False
        Catch ex As System.IO.IOException
            MessageBox.Show(ex.Message)
            Return False
        End Try
        Return True
    End Function
    Private Sub Send_Packet(Cam As Int16, Packet As Byte())

        Dim myReadBuffer As Byte() = SendCommand(Cam, Packet, True)
    End Sub

    Private Sub Do_Preset(Cam As Byte, Number As Byte, SetRecall As Byte, Remote As Boolean)
        Dim location As String = "Home"
        Dim index As String = "B"
        If Cam = Cam1 Then index = "A"
        If Number > 0 Then
            EditButton = Me.FindName("Button" + Number.ToString() + index)
            location = EditButton.Content
        End If
        If Remote Then
            index = "Moved to " & location & " by OBS"
        Else
            index = "Moved to " & location
        End If

        If (Cam = Cam1) Then Command1.Content = index
        If (Cam = Cam2) Then Command2.Content = index

        If Number = 0 Then
            Send_Packet(Cam, {&H81, &H1, &H6, &H4, &HFF}) ' Goto Home
            Return
        Else
            StartEffect()
            If SetRecall = 1 Then
                Send_Packet(Cam, {&H81, &H1, &H4, &H3F, 0, Number - 1, &HFF})  'Clear Preset
                Send_Packet(Cam, {&H81, &H1, &H4, &H3F, 1, Number - 1, &HFF})  'Set Preset		
                Send_Packet(Cam, {&H81, &H1, &H7E, &H1, &HB, Number - 1, &H18, &HFF}) 'Set Fast Move for Preset
            Else
                Send_Packet(Cam, {&H81, &H1, &H4, &H3F, 2, Number - 1, &HFF})  'Do Preset		
                GetZoom(Cam)
            End If
        End If
        Return
    End Sub
    Private Sub WritePresets()
        Dim FILE_NAME As String = TempPath + "SCPresets.dat"
        Dim presets As String = ""
        Dim Tries = 100
        Dim objWriter As System.IO.StreamWriter
        Dim index As String
        Do While Tries > 0
            Try
                objWriter = New System.IO.StreamWriter(FILE_NAME)
            Catch ex As System.IO.IOException
                Tries -= 1
                Threading.Thread.Sleep(500)
                Continue Do
            End Try
            If Tries > 0 Then

                For i = 1 To 5
                    If My.Settings.Disabled(i - 1) = "True" Then
                        presets += "--Disabled--,"
                    Else
                        presets += (My.Settings.Titles(i - 1) + ",")
                    End If
                Next
                For i = 1 To 5
                    index = "Cam" + i.ToString() + "Buttons"
                    For j = 1 To 14
                        presets += My.Settings(index)(j - 1)
                        If i < 5 Or j < 14 Then
                            presets += ","
                        End If
                    Next
                Next
                objWriter.Write(presets)
                objWriter.Close()
                Return
            Else
                objWriter.Close()
                Return
            End If
        Loop
    End Sub


    Private Sub ButtonTextKeyDown(sender As Object, e As KeyEventArgs) Handles ButtonText.PreviewKeyDown
        If e.Key = 6 Or e.Key = Key.Escape Then
            Dim Preset As String = EditButton.Uid
            Dim PresetNo As Short = CInt(Preset)
            Dim indexLabel As String
            Dim NewValue As String = ButtonText.Text
            Dim Cam = Cam1
            Dim P = Preset
            If Preset > 14 Then
                Cam = Cam2
                P -= 14
            End If
            indexLabel = "Cam" + Cam.ToString + "Buttons"
            Dim request = New TraversalRequest(FocusNavigationDirection.First)
            request.Wrapped = True
            ButtonText.MoveFocus(request)
            ButtonText.IsEnabled = False
            ButtonText.Visibility = Visibility.Hidden
            If NewValue <> "" Then
                If NewValue.Length > 13 Then NewValue = NewValue.Substring(0, 13)
                My.Settings.PropertyValues.Item(indexLabel).PropertyValue(P - 1) = NewValue
                My.Settings.Save()
                EditButton.Content = NewValue
                WritePresets()
                Do_Preset(Cam, P, 0, False) 'Clear Memory
                Do_Preset(Cam, P, 1, False) 'Set Memory
            End If
            Rename_State = False
            Rename_Button.Content = "Rename"
            Rename_Button2.Content = "Rename"
        End If
    End Sub
    Private Sub DoDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles Button1A.PreviewMouseDoubleClick, Button6A.PreviewMouseDoubleClick, Button2A.PreviewMouseDoubleClick, Button7A.PreviewMouseDoubleClick, Button3A.PreviewMouseDoubleClick, Button8A.PreviewMouseDoubleClick, Button4A.PreviewMouseDoubleClick, Button9A.PreviewMouseDoubleClick, Button5A.PreviewMouseDoubleClick, Button10A.PreviewMouseDoubleClick, Button11A.PreviewMouseDoubleClick, Button12A.PreviewMouseDoubleClick, Button13A.PreviewMouseDoubleClick, Button14A.PreviewMouseDoubleClick, Button1B.PreviewMouseDoubleClick, Button6B.PreviewMouseDoubleClick, Button2B.PreviewMouseDoubleClick, Button7B.PreviewMouseDoubleClick, Button3B.PreviewMouseDoubleClick, Button8B.PreviewMouseDoubleClick, Button4B.PreviewMouseDoubleClick, Button9B.PreviewMouseDoubleClick, Button10B.PreviewMouseDoubleClick, Button11B.PreviewMouseDoubleClick, Button12B.PreviewMouseDoubleClick, Button13B.PreviewMouseDoubleClick, Button14B.PreviewMouseDoubleClick, Button5B.PreviewMouseDoubleClick
        Dim Preset As String = CType(sender, Button).Uid
        Dim PresetNo As Short = CInt(Preset)
        EditButton = CType(sender, Button)
        Grid.SetRow(ButtonText, Grid.GetRow(EditButton))
        Grid.SetColumn(ButtonText, Grid.GetColumn(EditButton))
        ButtonText.Visibility = Visibility.Visible
        ButtonText.Text = ""
        ButtonText.IsEnabled = True
        ButtonText.Focus()
        e.Handled = True
        Return
    End Sub
    Private Sub Preset_Click(sender As Object, e As RoutedEventArgs) Handles Button1A.Click, Button6A.Click, Button2A.Click, Button7A.Click, Button3A.Click, Button8A.Click, Button4A.Click, Button9A.Click, Button5A.Click, Button10A.Click, Button11A.Click, Button12A.Click, Button13A.Click, Button14A.Click, Button1B.Click, Button6B.Click, Button2B.Click, Button7B.Click, Button3B.Click, Button8B.Click, Button4B.Click, Button9B.Click, Button10B.Click, Button11B.Click, Button12B.Click, Button13B.Click, Button14B.Click, Button5B.Click
        Dim Preset As String = CType(sender, Button).Uid
        Dim PresetNo As Short = CInt(Preset)
        EditButton = CType(sender, Button)
        Dim Cam = Cam1
        Dim P = Preset
        If P > 14 Then
            P -= 14
            Cam = Cam2
        End If
        If Rename_State Then
            Grid.SetRow(ButtonText, Grid.GetRow(EditButton))
            Grid.SetColumn(ButtonText, Grid.GetColumn(EditButton))
            ButtonText.Visibility = Visibility.Visible
            ButtonText.Text = ""
            ButtonText.IsEnabled = True
            ButtonText.Focus()
            Return
        ElseIf (SavePreset_State) Then
            SavePreset_State = False
            Save_Button.Content = "Set"
            Save_Button2.Content = "Set"
            Do_Preset(Cam, P, 1, False) 'Set Memory
        Else
            Do_Preset(Cam, P, 2, False) 'Recall Memory
        End If
    End Sub
    Private Sub SetCams()
        Cam1 = My.Settings.Cam1
        Cam2 = My.Settings.Cam2
        GetCamOnOff(Cam1)
        GetCamOnOff(Cam2)
        SetOnOff(Cam1)
        SetOnOff(Cam2)

        Dim index As String
        Dim Cam As Primitives.ToggleButton
        Dim item As String = "Cam" + Cam1.ToString() + "Buttons"
        Dim P As String = "A"
        Dim i As Byte
        Dim j As Byte
        Dim k As Byte
        For j = 1 To 2
            If j = 2 Then
                P = "B"
                item = "Cam" + Cam2.ToString() + "Buttons"
            End If
            For i = 1 To 14
                index = "Button" + i.ToString() + P
                EditButton = Me.FindName(index)
                EditButton.Content = My.Settings(item)(i - 1)
            Next
        Next
        Speed1.Value = My.Settings.CamSpeed(Cam1 - 1)
        Speed2.Value = My.Settings.CamSpeed(Cam2 - 1)
        Camera1_Heading.Content = My.Settings.Titles(Cam1 - 1)
        Camera2_Heading.Content = My.Settings.Titles(Cam2 - 1)
        Backlight1.IsChecked = My.Settings.Backlighting(Cam1 - 1)
        Backlight2.IsChecked = My.Settings.Backlighting(Cam2 - 1)
        Compensate1.IsChecked = My.Settings.Compensation(Cam1 - 1)
        Compensate2.IsChecked = My.Settings.Compensation(Cam2 - 1)
        StaySingle = My.Settings.StaySingle
        SetSingleDual()
        'setup cameras

        Dim CameraCnt As Byte
        For j = 1 To 2
            CameraCnt = 0
            For i = 1 To 5
                Cam = Me.FindName("CAM" + j.ToString + i.ToString)
                If My.Settings.Disabled(i - 1) = "False" Then CameraCnt += 1
                Cam.IsEnabled = False
                Cam.Visibility = Visibility.Hidden
                If j = 1 Then
                    Cam.IsChecked = (i = Cam1)
                Else
                    Cam.IsChecked = (i = Cam2)
                End If
            Next
        Next
        If CameraCnt = 0 Then
            DoConfig()
            Return
        Else
            Controls.Visibility = Visibility.Visible
            CameraSelects.Visibility = Visibility.Visible
            Configuration.Visibility = Visibility.Hidden
            settings.Content = "Settings"
            SettingsActive = False
        End If
        SingleCam = (CameraCnt = 1)

        If (CameraCnt = 2 And Not StaySingle) Or CameraCnt = 1 Then
            DoShrink()
        Else
            DoGrow()
        End If

        For j = 1 To 2
            k = 5 - CameraCnt
            For i = 1 To 5
                If My.Settings.Disabled(i - 1) = "False" Then
                    Cam = Me.FindName("CAM" + j.ToString + i.ToString)
                    Grid.SetColumn(Cam, (11 * (j - 1)) + k)
                    Cam.Visibility = Visibility.Visible
                    Cam.IsEnabled = True
                    k += 2
                End If
            Next
        Next
    End Sub
    Private Sub UpdatePresets()
        'set files ready
        Dim FILE_NAME As String
        For i = 1 To 5
            FILE_NAME = TempPath + "Preset" + i.ToString() + ".dat"
            System.IO.File.Delete(FILE_NAME)
            FILE_NAME = TempPath + "Preset" + i.ToString() + ".inf"
            System.IO.File.Delete(FILE_NAME)
            Command1.Content = ""
            Command2.Content = ""
        Next
        WritePresets()
    End Sub
    Private Function NibbleNum(X As Byte) As Byte()
        Dim Nibbles(4) As Byte
        Nibbles(0) = (X \ 10)
        Nibbles(1) = X - (Nibbles(0) * 10)
        Nibbles(2) = 0
        Nibbles(3) = 0
        Return Nibbles
    End Function
    Private Sub Do_Button1_Release(sender As Object, e As MouseButtonEventArgs) Handles C1UP.PreviewMouseLeftButtonUp, C1UR.PreviewMouseLeftButtonUp, C1RT.PreviewMouseLeftButtonUp, C1DR.PreviewMouseLeftButtonUp, C1DN.PreviewMouseLeftButtonUp, C1DL.PreviewMouseLeftButtonUp, C1LT.PreviewMouseLeftButtonUp, C1UL.PreviewMouseLeftButtonUp
        Send_Packet(Cam1, {&H81, &H1, &H6, &H1, Speed1.Value, Speed1.Value, 3, 3, &HFF})
        Shuttle1 = False
    End Sub

    Private Sub Do_Button2_Release(sender As Object, e As MouseButtonEventArgs) Handles C2UP.PreviewMouseLeftButtonUp, C2UR.PreviewMouseLeftButtonUp, C2RT.PreviewMouseLeftButtonUp, C2DR.PreviewMouseLeftButtonUp, C2DN.PreviewMouseLeftButtonUp, C2DL.PreviewMouseLeftButtonUp, C2LT.PreviewMouseLeftButtonUp, C2UL.PreviewMouseLeftButtonUp
        Send_Packet(Cam2, {&H81, &H1, &H6, &H1, Speed2.Value, Speed2.Value, 3, 3, &HFF})
        Shuttle2 = False
    End Sub
    Private Sub Do_Button1_Down(sender As Object, e As MouseButtonEventArgs) Handles C1UP.PreviewMouseLeftButtonDown, C1UR.PreviewMouseLeftButtonDown, C1RT.PreviewMouseLeftButtonDown, C1DR.PreviewMouseLeftButtonDown, C1DN.PreviewMouseLeftButtonDown, C1DL.PreviewMouseLeftButtonDown, C1LT.PreviewMouseLeftButtonDown, C1UL.PreviewMouseLeftButtonDown

        Dim outStream As Byte() = {&H81, &H1, &H6, &H1, Speed1.Value, Speed1.Value, Direction0(CInt(CType(sender, Button).Uid) - 1), Direction1(CInt(CType(sender, Button).Uid) - 1), &HFF}
        Send_Packet(Cam1, outStream)
    End Sub
    Private Sub Do_Button2_Down(sender As Object, e As MouseButtonEventArgs) Handles C2UP.PreviewMouseLeftButtonDown, C2UR.PreviewMouseLeftButtonDown, C2RT.PreviewMouseLeftButtonDown, C2DR.PreviewMouseLeftButtonDown, C2DN.PreviewMouseLeftButtonDown, C2DL.PreviewMouseLeftButtonDown, C2LT.PreviewMouseLeftButtonDown, C2UL.PreviewMouseLeftButtonDown
        Dim outStream As Byte() = {&H81, &H1, &H6, &H1, Speed2.Value, Speed2.Value, Direction0(CInt(CType(sender, Button).Uid) - 1), Direction1(CInt(CType(sender, Button).Uid) - 1), &HFF}
        Send_Packet(Cam2, outStream)
    End Sub
    Private Sub ZoomValue1(sender As Object, e As MouseButtonEventArgs) Handles Zoom1.PreviewMouseUp
        Dim zoomFactor As Byte() = NibbleNum(Zoom1.Value)
        Dim outStream As Byte() = {&H81, &H1, &H4, &H47, zoomFactor(0), zoomFactor(1), 1, 0, &HFF}
        Send_Packet(Cam1, outStream)
    End Sub
    Private Sub ZoomValue2(sender As Object, e As MouseButtonEventArgs) Handles Zoom2.PreviewMouseUp
        Dim zoomFactor As Byte() = NibbleNum(Zoom2.Value)
        Dim outStream As Byte() = {&H81, &H1, &H4, &H47, zoomFactor(0), zoomFactor(1), 1, 0, &HFF}
        Send_Packet(Cam2, outStream)
    End Sub
    Private Sub Do_UpdateConfig(sender As Object, e As MouseButtonEventArgs) Handles Speed1.PreviewMouseUp, Speed2.PreviewMouseUp
        My.Settings.CamSpeed(Cam1 - 1) = Speed1.Value
        My.Settings.CamSpeed(Cam2 - 1) = Speed2.Value
        My.Settings.Save()
    End Sub
    Private Sub DoAutofocus2(sender As Object, e As RoutedEventArgs) Handles AutoFocus2.Checked, AutoFocus2.Unchecked
        Dim outStream As Byte() = {&H81, &H1, &H4, &H38, &H2, &HFF}
        If AutoFocus2.IsChecked Then
            outStream(4) = &H3
        End If
        Send_Packet(Cam2, outStream)
    End Sub
    Private Sub DoAutofocus1(sender As Object, e As RoutedEventArgs) Handles AutoFocus1.Checked, AutoFocus1.Unchecked
        Dim outStream As Byte() = {&H81, &H1, &H4, &H38, &H2, &HFF}
        If AutoFocus1.IsChecked Then
            outStream(4) = &H3
        End If
        Send_Packet(Cam1, outStream)
    End Sub
    Private Sub DoComp2(sender As Object, e As RoutedEventArgs) Handles Compensate2.Checked, Compensate2.Unchecked
        Dim outStream As Byte() = {&H81, &H1, &H4, &H33, &H3, &HFF}
        Dim BLState As Boolean = False
        If Compensate1.IsChecked Then
            BLState = True
            outStream(4) = &H2
        End If
        Send_Packet(Cam2, outStream)
        My.Settings.Compensation(Cam2 - 1) = BLState
        My.Settings.Save()
    End Sub
    Private Sub DoComp1(sender As Object, e As RoutedEventArgs) Handles Compensate1.Checked, Compensate1.Unchecked
        Dim outStream As Byte() = {&H81, &H1, &H4, &H33, &H3, &HFF}
        Dim BLState As Boolean = False
        If Compensate2.IsChecked Then
            BLState = True
            outStream(4) = &H2
        End If
        Send_Packet(Cam1, outStream)
        My.Settings.Compensation(Cam1 - 1) = BLState
        My.Settings.Save()
    End Sub
    Private Sub DoFocus2Plus(sender As Object, e As MouseButtonEventArgs) Handles Focus1P.PreviewMouseDown, Focus2P.PreviewMouseDown
        Dim outStream As Byte() = {&H81, &H1, &H4, &H8, &H3, &HFF}
        If CType(sender, Button).Uid = "1" Then
            Send_Packet(Cam1, outStream)
        Else
            Send_Packet(Cam2, outStream)
        End If
    End Sub
    Private Sub DoFocus2Minus(sender As Object, e As MouseButtonEventArgs) Handles Focus1M.PreviewMouseDown, Focus2M.PreviewMouseDown
        Dim outStream As Byte() = {&H81, &H1, &H4, &H8, &H2, &HFF}
        If CType(sender, Button).Uid = "1" Then
            Send_Packet(Cam1, outStream)
        Else
            Send_Packet(Cam2, outStream)
        End If
    End Sub
    Private Sub DoFocus1Plus(sender As Object, e As MouseButtonEventArgs) Handles Focus1P.PreviewMouseUp, Focus2P.PreviewMouseUp, Focus1M.PreviewMouseUp, Focus2M.PreviewMouseUp
        Dim outStream As Byte() = {&H81, &H1, &H4, &H7, &H0, &HFF}
        Send_Packet(1, outStream)
    End Sub
    Private Sub DoBacklight1(sender As Object, e As RoutedEventArgs) Handles Backlight1.Checked, Backlight1.Unchecked
        Dim outStream As Byte() = {&H81, &H1, &H4, &H33, &H3, &HFF}
        Dim BLState As Boolean = False
        If Backlight1.IsChecked Then
            BLState = True
            outStream(4) = &H2
        End If
        Send_Packet(Cam1, outStream)
        My.Settings.Backlighting(Cam1 - 1) = BLState
        My.Settings.Save()
    End Sub
    Private Sub DoBacklight2(sender As Object, e As RoutedEventArgs) Handles Backlight2.Checked, Backlight2.Unchecked
        Dim outStream As Byte() = {&H81, &H1, &H4, &H33, &H3, &HFF}
        Dim BLState As Boolean = False
        If Backlight2.IsChecked Then
            BLState = True
            outStream(4) = &H2
        End If
        Send_Packet(Cam2, outStream)
        My.Settings.Backlighting(Cam2 - 1) = BLState
        My.Settings.Save()
    End Sub
    Private Sub DoHome2(sender As Object, e As MouseButtonEventArgs) Handles C2HM.MouseDoubleClick
        Do_Preset(Cam2, 0, 0, False)
    End Sub
    Private Sub DoHome1(sender As Object, e As MouseButtonEventArgs) Handles C1HM.MouseDoubleClick
        Do_Preset(Cam1, 0, 0, False)
    End Sub
    Private Sub DOC1DZM(sender As Object, e As MouseButtonEventArgs) Handles D1Plus.PreviewMouseDown, D1Minus.PreviewMouseDown
        Dim Direction As String = CType(sender, Image).Uid
        If (Direction = "1") Then
            Send_Packet(Cam1, {&H81, &H1, &H4, &H7, &H20, &HFF})
        Else
            Send_Packet(Cam1, {&H81, &H1, &H4, &H7, &H30, &HFF})
        End If
    End Sub
    Private Sub DOC2DZM(sender As Object, e As MouseButtonEventArgs) Handles D2Plus.PreviewMouseDown, D2Minus.PreviewMouseDown
        Dim Direction As String = CType(sender, Image).Uid
        If (Direction = "1") Then
            Send_Packet(Cam2, {&H81, &H1, &H4, &H7, &H20, &HFF})
        Else
            Send_Packet(Cam2, {&H81, &H1, &H4, &H7, &H30, &HFF})
        End If
    End Sub
    Private Sub DOC1DZSTP(sender As Object, e As MouseButtonEventArgs) Handles D1Plus.PreviewMouseUp, D1Minus.PreviewMouseUp
        Send_Packet(Cam1, {&H81, &H1, &H4, &H7, &H0, &HFF})
        GetZoom(Cam1)
    End Sub
    Private Sub DOC2DZSTP(sender As Object, e As MouseButtonEventArgs) Handles D2Plus.PreviewMouseUp, D2Minus.PreviewMouseUp
        Send_Packet(Cam2, {&H81, &H1, &H4, &H7, &H0, &HFF})
        GetZoom(Cam2)
    End Sub
    Private Sub DoZoomC120(sender As Object, e As MouseButtonEventArgs) Handles Zoom1.PreviewMouseDoubleClick
        Zoom1.Value = 20
        Dim zoomFactor As Byte() = NibbleNum(Zoom1.Value)
        Dim outStream As Byte() = {&H81, &H1, &H4, &H47, zoomFactor(0), zoomFactor(1), 1, 0, &HFF}
        Send_Packet(Cam1, outStream)
    End Sub
    Private Sub DoZoomC220(sender As Object, e As MouseButtonEventArgs) Handles Zoom2.PreviewMouseDoubleClick
        Zoom2.Value = 20
        Dim zoomFactor As Byte() = NibbleNum(Zoom2.Value)
        Dim outStream As Byte() = {&H81, &H1, &H4, &H47, zoomFactor(0), zoomFactor(1), 1, 0, &HFF}
        Send_Packet(Cam2, outStream)
    End Sub

    Private Sub DoCam1Checked(sender As Object, e As RoutedEventArgs) Handles CAM11.Checked, CAM12.Checked, CAM13.Checked, CAM14.Checked, CAM15.Checked
        Dim CamB As Primitives.ToggleButton = CType(sender, Primitives.ToggleButton)
        Dim NewCam1 = CInt(CamB.Uid)
        If NewCam1 = Cam2 Then
            Cam2 = Cam1
            My.Settings.Cam2 = Cam2
        End If
        Cam1 = NewCam1
        My.Settings.Cam1 = Cam1
        SetCams()
        My.Settings.Save()
    End Sub
    Private Sub DoCam2Checked(sender As Object, e As RoutedEventArgs) Handles CAM21.Checked, CAM22.Checked, CAM23.Checked, CAM24.Checked, CAM25.Checked
        Dim CamB As Primitives.ToggleButton = CType(sender, Primitives.ToggleButton)
        Dim NewCam2 = CInt(CamB.Uid)
        If NewCam2 = Cam1 Then
            Cam1 = Cam2
            My.Settings.Cam1 = Cam1
        End If
        Cam2 = NewCam2
        My.Settings.Cam2 = Cam2
        SetCams()
        My.Settings.Save()
    End Sub
    Private Sub Dosettings(sender As Object, e As RoutedEventArgs) Handles settings.Click
        If Not SettingsActive Then
            Presets.Width = 455
            Presets.MinWidth = 455
            Presets.MaxWidth = 455
            Grid.SetColumn(Menu, 2)
            DoConfig()
        Else
            DoControls()
            UpdatePresets()
            SetCams()
        End If
    End Sub
    Private Sub DoControls()
        Controls.Visibility = Visibility.Visible
        CameraSelects.Visibility = Visibility.Visible
        Configuration.Visibility = Visibility.Hidden
        SaveSettings.Visibility = Visibility.Hidden
        settings.Content = "Settings"
        settings.Background = Brush1
        SettingsActive = False
        Dim index As Byte
        Dim item As String
        For index = 0 To 4
            My.Settings.Item("Titles")(index) = CType(Me.FindName("Camera" + (index + 1).ToString() + "Name"), TextBox).Text
            My.Settings.Item("Disabled")(index) = Not CType(Me.FindName("Camera" + (index + 1).ToString() + "Enable"), CheckBox).IsChecked
            My.Settings.Item("CamIP")(index) = CType(Me.FindName("CameraIP" + (index + 1).ToString()), TextBox).Text
            My.Settings.Item("CamPort")(index) = CType(Me.FindName("CameraPort" + (index + 1).ToString()), TextBox).Text
            My.Settings.Item("CamCom")(index) = CType(Me.FindName("CameraCom" + (index + 1).ToString()), ComboBox).SelectedItem
            My.Settings.Item("CamAdd")(index) = CType(Me.FindName("CamAdd" + (index + 1).ToString()), Slider).Value.ToString
            My.Settings.Item("SerialIP")(index) = CType(Me.FindName("SerialIP" + (index + 1).ToString()), ComboBox).SelectedIndex.ToString
        Next

        If My.Settings.Item("Disabled")(Cam1 - 1) = "True" Then Cam1 = FindCam(1)
        If My.Settings.Item("Disabled")(Cam2 - 1) = "True" Or Cam2 = Cam1 Then
            Cam2 = FindCam(2)
            If (Cam2 < Cam1) Then
                Dim t As Byte = Cam2
                Cam2 = Cam1
                Cam1 = t
            End If
        End If
        If Cam1 = Cam2 Then SingleCam = True
        My.Settings.Cam1 = Cam1
        My.Settings.Cam2 = Cam2
        My.Settings.Save()
        SetCams()
    End Sub
    Private Function FindCam(target As Byte) As Byte
        ' Leave Cam1 alone if enabled
        If target = 1 And My.Settings.Disabled(Cam1 - 1) = "False" Then Return Cam1
        'leave Cam2 alone if enabled and not the same as Cam1
        If target = 2 And Cam2 <> Cam1 And My.Settings.Disabled(Cam2 - 1) = "False" Then Return Cam2
        'return Cam2 for Cam1 if possible
        If target = 1 And My.Settings.Disabled(Cam2 - 1) = "False" Then Return Cam2
        ' find any enabled camera for target
        Dim c = 5
        While (c > 1) And (My.Settings.Disabled(c - 1) = "True" Or (c = Cam1))
            c -= 1
        End While
        'return what is left
        Return c
    End Function
    Private Sub DoConfig()
        Dim index As Byte
        For index = 0 To 4
            CType(Me.FindName("Camera" + (index + 1).ToString() + "Name"), TextBox).Text = My.Settings.Item("Titles")(index)
            CType(Me.FindName("Camera" + (index + 1).ToString() + "Enable"), CheckBox).IsChecked = My.Settings.Item("Disabled")(index) = "False"
            CType(Me.FindName("CameraIP" + (index + 1).ToString()), TextBox).Text = My.Settings.Item("CamIP")(index)
            CType(Me.FindName("CameraPort" + (index + 1).ToString()), TextBox).Text = My.Settings.Item("CamPort")(index)
            CType(Me.FindName("CameraCom" + (index + 1).ToString()), ComboBox).SelectedItem = My.Settings.Item("CamCom")(index)
            CType(Me.FindName("CamAdd" + (index + 1).ToString()), Slider).Value = CInt(My.Settings.Item("CamAdd")(index))
            CType(Me.FindName("SerialIP" + (index + 1).ToString()), ComboBox).SelectedIndex = CInt(My.Settings.Item("SerialIP")(index))
            SetSerialIP((index + 1).ToString, CType(Me.FindName("SerialIP" + (index + 1).ToString()), ComboBox).SelectedIndex)
        Next
        Controls.Visibility = Visibility.Hidden
        CameraSelects.Visibility = Visibility.Hidden
        Configuration.Visibility = Visibility.Visible
        SaveSettings.Visibility = Visibility.Visible
        settings.Content = "Controls"
        settings.Background = Brush2
        SettingsActive = True
        Presets1.RowDefinitions(1).Height = New GridLength(40)
        Presets.MaxHeight = 700
        Presets.MinHeight = 700
        Presets.Height = 700
    End Sub
    Private Sub SetHeightwidth()
        Dim max As Int16
        If CamSelect Then
            CameraSelects.Visibility = Visibility.Visible
            Presets1.RowDefinitions(1).Height = New GridLength(40)
            max = 700
        Else
            CameraSelects.Visibility = Visibility.Collapsed
            Presets1.RowDefinitions(1).Height = New GridLength(0)
            max = 660
        End If

        Presets.MaxHeight = max
        Presets.MinHeight = max
        Presets.Height = max

        If SingleCam Or StaySingle Then
            Presets.Width = 235
            Presets.MinWidth = 235
            Presets.MaxWidth = 235
            Grid.SetColumn(Menu, 0)
        Else
            Presets.Width = 455
            Presets.MinWidth = 455
            Presets.MaxWidth = 455
            Grid.SetColumn(Menu, 2)
        End If
    End Sub

    Private Sub DoShrink()
        CamSelect = False
        SetHeightwidth()
    End Sub
    Private Sub DoGrow()
        CamSelect = True
        SetHeightWidth()
    End Sub

    Private Sub Do_Home_All(sender As Object, e As MouseButtonEventArgs) Handles homeall.PreviewMouseDoubleClick
        Dim i As Byte
        For i = 1 To 5
            If My.Settings.Disabled(i - 1) = "False" Then
                Do_Preset(i, 0, 0, False)
            End If
        Next
    End Sub
    Private Sub SetSingleDual()
        If Not StaySingle Then
            singledual.Background = Brush2
            singledual.Content = "Single"
        Else
            singledual.Background = Brush1
            singledual.Content = "Dual"
        End If
    End Sub
    Private Sub Do_SingleDual(sender As Object, e As RoutedEventArgs) Handles singledual.Click
        If Not SettingsActive Then
            StaySingle = Not StaySingle
            My.Settings.StaySingle = StaySingle
            My.Settings.Save()
            SetSingleDual()
            SetCams()
        End If

    End Sub

    Private Sub SetSerialIP(Cam As String, Tag As Byte)
        Dim IPCid As TextBox = Me.FindName("CameraIP" + Cam)
        Dim PORTCid As TextBox = Me.FindName("CameraPort" + Cam)
        Dim ADDCid As Grid = Me.FindName("CameraAdd" + Cam)
        Dim COMCid As ComboBox = Me.FindName("CameraCom" + Cam)
        If Tag = 0 Then
            IPCid.Visibility = Visibility.Visible
            PORTCid.Visibility = Visibility.Visible
            ADDCid.Visibility = Visibility.Hidden
            COMCid.Visibility = Visibility.Hidden
        Else
            IPCid.Visibility = Visibility.Hidden
            PORTCid.Visibility = Visibility.Hidden
            ADDCid.Visibility = Visibility.Visible
            COMCid.Visibility = Visibility.Visible
        End If
    End Sub

    Private Sub DoSerialIP(sender As Object, e As SelectionChangedEventArgs) Handles SerialIP1.SelectionChanged, SerialIP2.SelectionChanged, SerialIP3.SelectionChanged, SerialIP4.SelectionChanged, SerialIP5.SelectionChanged
        SetSerialIP(CType(sender, ComboBox).Uid, CType(sender, ComboBox).SelectedIndex)
    End Sub

    Private Sub DoSettingSave(sender As Object, e As RoutedEventArgs)
        Send_Packet(Cam2, {&H88, &H30, &H2, &HFF}) 
        DoControls()
        UpdatePresets()
        SetCams()
    End Sub
    Private Sub SetOnOff(Cam)
        Dim CamB As Button
        If Cam = Cam1 Then CamB = CamOnOffButton(0)
        If Cam = Cam2 Then CamB = CamOnOffButton(1)
        If CamOn(Cam - 1) Then
            CamB.Background = Brush4
            CamB.Content = "On"
            If Cam = Cam1 Then Ctrl1.Visibility = Visibility.Visible
            If Cam = Cam2 Then Ctrl2.Visibility = Visibility.Visible
        Else
            CamB.Background = Brush3
            CamB.Content = "Off"
            If Cam = Cam1 Then Ctrl1.Visibility = Visibility.Hidden
            If Cam = Cam2 Then Ctrl2.Visibility = Visibility.Hidden
        End If
    End Sub
    Private Sub DoCamOnOff(sender As Object, e As RoutedEventArgs) Handles CamOnOff1.Click, CamOnOff2.Click

        Dim CamB As Button = CType(sender, Button)
        Dim Cam = CInt(CamB.Uid)
        CamB = CamOnOffButton(Cam - 1)
        If Cam = 1 Then
            Cam = Cam1
        Else
            Cam = Cam2
        End If
        If CamOn(Cam - 1) Then
            If Not My.Computer.Keyboard.CtrlKeyDown Then Return
            Send_Packet(Cam, {&H80, &H1, &H4, &H0, &H3, &HFF})
            GetCamOnOff(Cam)
        Else
            Send_Packet(Cam, {&H80, &H1, &H4, &H0, &H2, &HFF})
            GetCamOnOff(Cam)

        End If
        SetOnOff(Cam)
    End Sub
End Class