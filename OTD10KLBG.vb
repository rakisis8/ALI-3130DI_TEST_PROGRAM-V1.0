
Imports System.Windows.Forms
Imports System.IO
Imports Microsoft.Office.Interop.Excel
Imports Microsoft.Office.Interop
Imports System.Data.DataTable
Imports Excel = Microsoft.Office.Interop.Excel
Imports Microsoft.Office



Public Class OTD10KLBG

    Dim myPort As Array  'COM Ports detected on the system will be stored here

    Dim CNT As Integer
    Dim Path As String
    Dim data(8) As String
    Dim GoodCNT As Integer
    Dim NGCNT As Integer
    Dim Yield As Double


    Private Delegate Sub UpdateFormDelegate()
    Private UpdateFormDelegate1 As UpdateFormDelegate

    Private Sub BtnConnect_Click(sender As Object, e As EventArgs) Handles BtnConnect.Click

        Try

            If BtnConnect.Text = "Disconnected" Then

                If SerialPort1.PortName <> "" Then
                    SerialPort1.PortName = cmbPort.Text      'Set SerialPort1 to the selected COM port at startup

                    SerialPort1.Parity = IO.Ports.Parity.None
                    SerialPort1.StopBits = IO.Ports.StopBits.One
                    SerialPort1.DataBits = 8            'Open our serial port
                    SerialPort1.BaudRate = cmbBaud.Text
                    SerialPort1.Open()
                    BtnConnect.Text = "Connected"
                    BtnConnect.BackColor = Color.LimeGreen

                End If


            ElseIf BtnConnect.Text = "Connected" Then
                If SerialPort1.IsOpen = True Then
                    SerialPort1.Close()             'Close our Serial Port
                    BtnConnect.Text = "Disconnected"
                    BtnConnect.BackColor = Color.OrangeRed
                End If

            End If

        Catch ex As Exception

            MsgBox(ex.Message)

        End Try


    End Sub


    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        myPort = IO.Ports.SerialPort.GetPortNames() 'Get all com ports available


        For i = 0 To UBound(myPort)
            cmbPort.Items.Add(myPort(i))
            cmbPort.Text = cmbPort.Items.Item(0)

        Next


        BtnConnect.Text = "Disconnected"
        BtnConnect.BackColor = Color.OrangeRed

        cmbBaud.Text = 115200

        BtnStart.BackColor = Color.DodgerBlue


        '데이터그리드뷰 헤더 글씨 중앙 정렬
        DataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        DataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.DarkOrange

    End Sub



    Private Sub BtnStart_Click(sender As Object, e As EventArgs) Handles BtnStart.Click

        Try
            Dim openFileDialog1 As New OpenFileDialog()
            openFileDialog1.InitialDirectory = "C:\"
            openFileDialog1.RestoreDirectory = True
            openFileDialog1.ShowDialog()
            If openFileDialog1.FileName.Length = 0 Then

                'if the user selected a file set the value of the replacefile text box
            Else
                Path = System.IO.Path.GetFullPath(openFileDialog1.FileName)

            End If

            Dim fileReader As IO.StreamReader
            fileReader = My.Computer.FileSystem.OpenTextFileReader(Path)
            Dim stringReader As String

            While Not fileReader.EndOfStream
                stringReader = fileReader.ReadLine()
                SerialPort1.Write(stringReader & Chr(10))     '시리얼포트 통해 데이터 쓰기
            End While

        Catch ex As Exception

            MsgBox(ex.Message)

        End Try

    End Sub




    Private Sub UpdateDisplay()

        For k As Integer = 0 To 2
            RichTextBox1.Text += data(k)
        Next
        RichTextBox1.Text += vbCrLf
        DataOut()

    End Sub

    Private Sub DataOut()

        Judgment.Text = data(2)

        If Judgment.Text Like "*NG*" Then '입력값에 공백이 포함, Like 구문으로 NG 글자 확인 

            Judgment.BackColor = Color.Red
            NGCNT += 1

        Else
            Judgment.BackColor = Color.LawnGreen
            GoodCNT += 1
        End If

        Dim row As String() = New String() {DataGridView1.Rows.Count, data(0), data(1), data(2), data(3)}
        DataGridView1.Rows.Add(row)

        '스크롤 가장 최신행으로 업데이트
        DataGridView1.FirstDisplayedScrollingRowIndex = DataGridView1.Rows.Count - 1

        yeild()

    End Sub

    Private Sub yeild()

        Label4.Text = DataGridView1.Rows.Count - 1
        Label8.Text = GoodCNT
        Label9.Text = NGCNT
        Yield = (GoodCNT / (DataGridView1.Rows.Count - 1))
        Label10.Text = Format(Yield, "0.00%")

    End Sub

    Private Sub Delay(ByVal MiliSecond As Double)

        Dim delayTime As Date = Now.AddSeconds(MiliSecond / 1000)
        Do Until Now > delayTime
            System.Windows.Forms.Application.DoEvents()
        Loop

    End Sub


    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        Try
            If DataGridView1.Rows.Count - 1 > 0 Then

                '마지막 측정값 열 삭제
                CNT = DataGridView1.Rows.Count - 2
                If DataGridView1.Rows.Item(CNT).Cells(3).Value Like "*NG*" Then

                    NGCNT -= 1
                Else
                    GoodCNT -= 1

                End If
                DataGridView1.Rows.Remove(DataGridView1.Rows(CNT))
                yeild()
            Else
                MsgBox("데이터가 없습니다. ")
            End If

        Catch ex As Exception

            MsgBox(ex.Message)

        End Try

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        Try
            Dim objexcel As New Excel.Application()
            Dim objbook As Excel.Workbook = objexcel.Workbooks.Add()

            Me.DataGridView1.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableAlwaysIncludeHeaderText
            Me.DataGridView1.SelectAll()
            If Me.DataGridView1.GetCellCount(DataGridViewElementStates.Selected) > 0 Then
                Clipboard.SetDataObject(Me.DataGridView1.GetClipboardContent())
            End If


            Dim fName As String = SaveTextBox.Text
            Using sfd As New SaveFileDialog
                sfd.Title = "Save As"
                sfd.OverwritePrompt = True
                sfd.FileName = fName
                sfd.DefaultExt = ".xlsx"
                sfd.Filter = "Excel Workbook(*.xlsx)|"
                sfd.AddExtension = True
                If sfd.ShowDialog() = DialogResult.OK Then
                    objexcel.ActiveSheet.PasteSpecial()
                    objexcel.Cells.Select()
                    objexcel.Selection.WrapText = False
                    objexcel.Columns.AutoFit()
                    objbook.SaveAs(sfd.FileName)
                    objbook.Close()
                    objexcel.Quit()
                    objbook = Nothing
                    objexcel = Nothing
                    killexcel()
                    MsgBox("Export Successfully !", MsgBoxStyle.Information, "== Notice ==")
                End If
            End Using


        Catch ex As Exception
            MsgBox(ex.Message)
        End Try

    End Sub

    Private Sub killexcel()
        Try
            For Each proc As Process In Process.GetProcessesByName("EXCEL")
                If proc IsNot Nothing AndAlso proc.MainWindowTitle = "" Then
                    proc.Kill()
                End If
            Next
        Catch ex As Exception
            Throw ex
        End Try
    End Sub



    Private Sub SerialPort1_DataReceived(sender As Object, e As Ports.SerialDataReceivedEventArgs) Handles SerialPort1.DataReceived


        UpdateFormDelegate1 = New UpdateFormDelegate(AddressOf UpdateDisplay)
        Dim str() As String = SerialPort1.ReadLine.Split(",")

        For k As Integer = 0 To 2
            data(k) = str(k)
        Next

        Me.Invoke(UpdateFormDelegate1)
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        DataGridView1.Rows.Clear()
        NGCNT = 0
        GoodCNT = 0
        yeild()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click

        If DataGridView1.SelectedRows(0).Cells(0).Value = Nothing Then
            Return
        End If

        If DataGridView1.SelectedRows(0).Cells(3).Value Like "*NG*" Then

            NGCNT -= 1
        Else
            GoodCNT -= 1

        End If

        Dim msgResult As MsgBoxResult = MsgBox("삭제하시겠습니까?", MsgBoxStyle.YesNo)

        If msgResult = MsgBoxResult.Yes Then
            DataGridView1.Rows.RemoveAt(DataGridView1.SelectedRows(0).Index)

        End If

        yeild()
    End Sub
End Class
