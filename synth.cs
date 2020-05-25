using System;
using System.Drawing;
using System.Windows.Forms;
using PortAudio.Net;

using static System.Console;
using static System.Math;

class Synth {
    const double SampleRate = 44100;

    PaLibrary paLibrary = PaLibrary.Initialize();
    PaStream stream;

    // The note we are currently playing, or -1 if none.  0 is the note C2 = 65.41 Hz.
    public int note = -1;

    double osc, delta;

    public void setNote(int note) {
        this.note = note;
        double freq = 65.41 * Pow(2, note / 12.0);
        delta = freq * 2 * Math.PI / SampleRate;
    }

    PaStreamCallbackResult SineCallback(
        PaBuffer input, PaBuffer output,
        int frameCount, PaStreamCallbackTimeInfo timeInfo,
        PaStreamCallbackFlags statusFlags, object userData)
    {
        var outBuffer = (PaBuffer<float>)output;
        var outSpan = outBuffer.Span;
        if (note >= 0) {
            for (int n = 0; n < frameCount; n++) {
                outSpan[n] = (float) Sin(osc);
                osc += delta;
            }
            osc = osc % (2 * Math.PI);
        } else {
            for (int n = 0; n < frameCount; n++)
                outSpan[n] = 0;   // silence;
        }
        return PaStreamCallbackResult.paContinue;
    }

    public void start() {
        var device = paLibrary.DefaultOutputDevice;

        var outputParameters = new PaStreamParameters() {
            device = device,
            channelCount = 1,
            sampleFormat = PaSampleFormat.paFloat32,
            suggestedLatency = paLibrary.GetDeviceInfo(device).Value.defaultLowOutputLatency,
            hostApiSpecificStreamInfo = IntPtr.Zero
        };
        
        osc = 0;
        
        stream = paLibrary.OpenStream(
            null, outputParameters, SampleRate, 512, PaStreamFlags.paNoFlag,
            SineCallback, IntPtr.Zero);
        stream.StartStream();
    }

    public void stop() {
        stream.StopStream();
        stream.Dispose();
        stream = null;
    }
}

class View : Form {
    const int Keys = 50;
    const int KeyHeight = 95, KeyWidth = 25, Border = 20;
    const int BlackKeyHeight = 60, BlackKeyWidth = 16;
    
    int[] WhiteKeyOffsets = { 0, 2, 4, 5, 7, 9, 11 };
    int[] BlackKeyOffsets = { 1, 3, 0, 6, 8, 10, 0 };
    
    Synth synth;

    bool mouseDown = false;
    
    public View() {
        Text = "sine-synth";
        ClientSize = new Size(Keys * KeyWidth + 2 * Border, KeyHeight + 2 * Border);
        StartPosition = FormStartPosition.CenterScreen;

        synth = new Synth();
        synth.start();
    }

    void setNote(int note) {
        synth.setNote(note);
        Console.WriteLine("note = " + note);
    }

    protected override void OnPaint(PaintEventArgs args) {
        Graphics g = args.Graphics;
        g.TranslateTransform(Border, Border);
        
        // Draw a piano keyboard.
        
        g.DrawRectangle(Pens.Black, 0, 0, Keys * KeyWidth, KeyHeight);
        for (int k = 0 ; k < Keys ; ++k)
            g.DrawLine(Pens.Black, k * KeyWidth, 0, k * KeyWidth, KeyHeight);
        for (int k = 0 ; k < Keys - 1 ; ++k)
            if (k % 7 != 2 && k % 7 != 6)
                g.FillRectangle(Brushes.Black,
                                (k + 1f) * KeyWidth - 0.5f * BlackKeyWidth, 0f,
                                BlackKeyWidth, BlackKeyHeight);
    }

    // Convert an (x, y) pixel position to a key number.
    int xyToKey(int x, int y) {
        x -= Border;
        y -= Border;
        if (0 <= y && y < BlackKeyHeight) {
            int x1 = x + BlackKeyWidth / 2;
            if (x1 % KeyWidth < BlackKeyWidth) {
                int k = x1 / KeyWidth - 1;
                if (k % 7 != 2 && k % 7 != 6)  // on a black key
                    return 12 * (k / 7) + BlackKeyOffsets[k % 7];
            }
        }
        if (0 <= y && y <= KeyHeight) {
            int k = x / KeyWidth;
            return 12 * (k / 7) + WhiteKeyOffsets[k % 7];
        }
        return -1;
    }

    protected override void OnMouseDown(MouseEventArgs args) {
        setNote(xyToKey(args.X, args.Y));
        mouseDown = true;
    }
    
    protected override void OnMouseMove(MouseEventArgs args) {
        if (mouseDown) {
            int n = xyToKey(args.X, args.Y);
            if (n != synth.note)
                setNote(n);
        }
    }
    
    protected override void OnMouseUp(MouseEventArgs args) {
        setNote(-1);
        mouseDown = false;
    }

    protected override void OnClosed(EventArgs e) {
        synth.stop();
    }
}

class Hello {
    [STAThread]
    static void Main() {
        View view = new View();
        
        Application.Run(view);
    }
}
