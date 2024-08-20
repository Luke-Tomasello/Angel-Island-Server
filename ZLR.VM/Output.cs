using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Nito.AsyncEx;

namespace ZLR.VM
{
    public delegate void SoundFinishedCallback();

    /// <summary>
    /// Indicates whether a given character can be printed and/or received as input.
    /// </summary>
    /// <seealso cref="IZMachineIO.CheckUnicode"/>
    [Flags]
    public enum UnicodeCaps
    {
        /// <summary>
        /// Indicates that the character can be printed.
        /// </summary>
        CanPrint = 1,
        /// <summary>
        /// Indicates that the character can be received as input.
        /// </summary>
        CanInput = 2
    }

    /// <summary>
    /// Indicates the text style being selected in <see cref="IZMachineIO.SetTextStyle"/>.
    /// </summary>
    /// <remarks>
    /// Despite the power-of-two enum values, these styles are not bit flags, and the
    /// interface module is not expected to support setting multiple styles in a single call.
    /// </remarks>
    public enum TextStyle : ushort
    {
        /// <summary>
        /// Turns off all special text styles.
        /// </summary>
        Roman = 0,
        /// <summary>
        /// Reverses foreground and background colors.
        /// </summary>
        Reverse = 1,
        /// <summary>
        /// Boldface text.
        /// </summary>
        Bold = 2,
        /// <summary>
        /// Italic text.
        /// </summary>
        Italic = 4,
        /// <summary>
        /// Fixed pitch text.
        /// </summary>
        FixedPitch = 8
    }

    /// <summary>
    /// Indicates the action being requested by <see cref="IZMachineIO.PlaySoundSample"/>.
    /// </summary>
    public enum SoundAction : ushort
    {
        /// <summary>
        /// Cache the sound in anticipation of playing it soon.
        /// </summary>
        Prepare = 1,
        /// <summary>
        /// Start playing the sound in the background.
        /// </summary>
        Start = 2,
        /// <summary>
        /// Stop the sound if it's currently playing.
        /// </summary>
        Stop = 3,
        /// <summary>
        /// Evict the sound from the cache because it won't be needed again soon.
        /// </summary>
        FinishWith = 4
    }

    /// <summary>
    /// Indicates the reason why an input method returned.
    /// </summary>
    public enum ReadOutcome
    {
        /// <summary>
        /// Input was cancelled by the timer callback.
        /// </summary>
        Cancelled,

        /// <summary>
        /// Input was terminated by a keypress.
        /// </summary>
        KeyPressed,

        /// <summary>
        /// The user asked to break into the debugger.
        /// </summary>
        DebuggerBreak
    }

    /// <summary>
    /// Indicates the outcome of a call to <see cref="IAsyncZMachineIO.ReadLineAsync"/>.
    /// </summary>
    public struct ReadLineResult
    {
        public ReadOutcome Outcome { get; }

        [CanBeNull]
        private readonly string? text;

        private readonly byte terminator;

        [NotNull]
        public string Text
        {
            get
            {
                if (Outcome != ReadOutcome.KeyPressed)
                    throw new InvalidOperationException();

                Debug.Assert(text != null);
                // ReSharper disable once AssignNullToNotNullAttribute
                return text;
            }
        }

        public byte Terminator =>
            Outcome == ReadOutcome.KeyPressed ? terminator : throw new InvalidOperationException();

        private ReadLineResult(ReadOutcome outcome, [CanBeNull] string? text, byte terminator)
        {
            Outcome = outcome;
            this.text = text;
            this.terminator = terminator;
        }

        public override string ToString() =>
            this.Outcome == ReadOutcome.KeyPressed
                ? $"Outcome={this.Outcome}, Terminator={terminator}, Text=\"{text}\""
                : $"Outcome={this.Outcome}";

        /// <summary>
        /// Input was cancelled by the timer callback.
        /// </summary>
        public static readonly ReadLineResult Cancelled = new ReadLineResult(ReadOutcome.Cancelled, null, 0);

        /// <summary>
        /// The user asked to break into the debugger.
        /// </summary>
        public static readonly ReadLineResult DebuggerBreak = new ReadLineResult(ReadOutcome.DebuggerBreak, null, 0);

        /// <summary>
        /// The user entered text and pressed a terminating key. 
        /// </summary>
        /// <param name="text">The entered text.</param>
        /// <param name="terminator">The ZSCII code of the terminating key.</param>
        /// <returns>A structure describing the result of the read.</returns>
        public static ReadLineResult LineEntered([NotNull] string text, byte terminator = 13) => new ReadLineResult(ReadOutcome.KeyPressed, text, terminator);
    }

    /// <summary>
    /// Provides an interface for Z-machine I/O features: reading and writing text;
    /// opening streams for saved games and transcripts; playing sounds; moving the cursor
    /// and splitting windows; changing the text style; and indicating the capabilities of
    /// the I/O system.
    /// </summary>
    [PublicAPI]
    public interface IZMachineIO
    {
        // TODO: let the I/O module know whether we're using a command file, so it can disable the "more" prompts
        #region Input

        /// <summary>
        /// Reads a line of input from the player.
        /// </summary>
        /// <param name="initial">The initial string which has been supplied for the player's
        /// input, or an empty string if no initial input has been supplied.</param>
        /// <param name="time">The callback interval for timed input, in tenths of a second.
        /// If this is nonzero, <paramref name="callback"/> should be called every <paramref name="time"/>/10
        /// seconds.</param>
        /// <param name="callback">The callback function for timed input, which should be called
        /// every so often according to <paramref name="time"/>. The function can return true to cancel
        /// input immediately.</param>
        /// <param name="terminatingKeys">An array of ZSCII values of function keys which should
        /// terminate input immediately if pressed. The special value 255 means "any function key" and will
        /// appear alone.</param>
        /// <param name="allowDebuggerBreak"><b>true</b> if the function may break into the debugger by
        /// returning <see cref="ReadLineResult.DebuggerBreak"/>.</param>
        /// <returns>A <see cref="ReadLineResult"/> indicating how the line input request ended.</returns>
        /// <remarks>
        /// <para>If a non-empty string is supplied as <paramref name="initial"/>, the string will have
        /// already been printed by the game. The interface should avoid printing it again, but should
        /// still allow the player to edit it as if he had typed it himself. (If this cannot be achieved,
        /// it is recommended to err on the side of letting the player edit the text.)</para>
        /// </remarks>
        [Obsolete("Use the async method instead.")]
        ReadLineResult ReadLine([NotNull] string initial, int time, [NotNull] TimedInputCallback callback, byte[] terminatingKeys, bool allowDebuggerBreak);
        /// <summary>
        /// Reads a single key of input from the player, without echoing it.
        /// </summary>
        /// <param name="time">The callback interval for timed input, in tenths of a second.
        /// If this is nonzero, <paramref name="callback"/> should be called every <paramref name="time"/>/10
        /// seconds.</param>
        /// <param name="callback">The callback function for timed input, which should be called
        /// every so often according to <paramref name="time"/>. The function can return true to cancel
        /// input immediately.</param>
        /// <param name="translator">A helper callback which translates printable characters into their
        /// ZSCII values, according to the currently selected translation table.</param>
        /// <returns>The ZSCII value of the key that was pressed, or 0 if input was cancelled by the
        /// timer callback.</returns>
        [Obsolete("Use the async method instead.")]
        short ReadKey(int time, [NotNull] TimedInputCallback callback, [NotNull] CharTranslator translator);
        /// <summary>
        /// Displays a command that has been read from the command file.
        /// </summary>
        /// <param name="command">The command read from the file. If the command was terminated
        /// by pressing the enter key, this string will end with a newline.</param>
        void PutCommand([NotNull] string command);
        
        #endregion

        #region Output

        /// <summary>
        /// Writes a character to the screen, using the currently selected text style, cursor, and
        /// window settings.
        /// </summary>
        /// <param name="ch">The character to write.</param>
        void PutChar(char ch);
        /// <summary>
        /// Writes a string to the screen, using the currently selected text style, cursor, and
        /// window settings.
        /// </summary>
        /// <param name="str">The string to write.</param>
        void PutString([NotNull] string str);
        /// <summary>
        /// Writes a series of lines to the screen, spreading down and to the right from the
        /// current cursor position, and leaving the cursor at the end of the last line.
        /// </summary>
        /// <param name="lines">The lines to write.</param>
        void PutTextRectangle([ItemNotNull, NotNull] string[] lines);
        /// <summary>
        /// Gets or sets a value indicating whether text in the lower (main) window is
        /// buffered for word wrapping.
        /// </summary>
        /// <remarks>
        /// This value should be true initially when the game starts.
        /// The upper window is always buffered.
        /// </remarks>
        bool Buffering { get; set; }

        #endregion

        #region Transcripts

        /// <summary>
        /// Gets or sets a value indicating whether a transcript file is being written.
        /// </summary>
        /// <remarks>
        /// The interface module is responsible for prompting the player for a file name, if necessary.
        /// However, if the player has already selected a transcript file during the current game,
        /// the same file should be reused instead of prompting for another name, so games can
        /// turn transcripting off and on in rapid succession.
        /// </remarks>
        bool Transcripting { get; set; }
        /// <summary>
        /// Writes a single character to the transcript file.
        /// </summary>
        /// <param name="ch">The character to write.</param>
        void PutTranscriptChar(char ch);
        /// <summary>
        /// Writes a string to the transcript file.
        /// </summary>
        /// <param name="str">The string to write.</param>
        void PutTranscriptString([NotNull] string str);

        #endregion

        #region Saving the Game State

        /// <summary>
        /// Opens a stream to write the saved game file.
        /// </summary>
        /// <param name="size">The size of the game state that will be written, in bytes.</param>
        /// <returns>A writable <see cref="System.IO.Stream"/> for the save file, which the
        /// VM will close after it's done saving; or <see langword="null"/> if the user chose not to select a
        /// file or the file couldn't be opened.</returns>
        /// <remarks>
        /// The interface module is responsible for prompting the player for a file name, if necessary.
        /// </remarks>
        [CanBeNull]
        [Obsolete("Use the async method instead.")]
        Stream? OpenSaveFile(int size);
        /// <summary>
        /// Opens a stream to read a previously saved game file.
        /// </summary>
        /// <returns>A readable <see cref="System.IO.Stream"/> for the save file, which the
        /// VM will close after it's done loading; or <see langword="null"/> if the user chose not to select a
        /// file or the file couldn't be opened.</returns>
        /// <remarks>
        /// The interface module is responsible for prompting the player for a file name, if necessary.
        /// </remarks>
        [CanBeNull]
        [Obsolete("Use the async method instead.")]
        Stream? OpenRestoreFile();
        /// <summary>
        /// Opens a stream to read or write auxiliary game data.
        /// </summary>
        /// <param name="name">A suggested name for the auxiliary file.</param>
        /// <param name="size">The size, in bytes, of the array that will be read from or
        /// written to the auxiliary file.</param>
        /// <param name="writing">True if the stream will be used to save auxiliary data;
        /// false if it will be used to read previously saved data.</param>
        /// <returns>A <see cref="System.IO.Stream"/> for the auxiliary file, which must be
        /// readable or writable depending on the value of <paramref name="writing"/>, and
        /// which the VM will close after it's done using; or <see langword="null"/> if the user chose not to
        /// select a file or the file couldn't be opened.</returns>
        /// <remarks>
        /// The interface module is responsible for prompting the player for a file name, if necessary.
        /// The interface module may choose to use the suggested name as-is, or prompt the user
        /// for a name and use the suggested name as a default. The suggested name should at least
        /// be visible to the user, since a game may use several auxiliary files.
        /// </remarks>
        [CanBeNull]
        [Obsolete("Use the async method instead.")]
        Stream? OpenAuxiliaryFile([NotNull] string name, int size, bool writing);
        /// <summary>
        /// Opens a stream to read or write the player's input to a file.
        /// </summary>
        /// <param name="writing">True if the stream will be used to record the player's
        /// input; false if it will be used to replay previously recorded input.</param>
        /// <returns>A <see cref="System.IO.Stream"/> for the command file, which must be
        /// readable or writable depending on the value of <paramref name="writing"/>, and
        /// which the VM will close after it's done using; or <see langword="null"/> if the user chose not to
        /// select a file or the file couldn't be opened.</returns>
        [CanBeNull]
        [Obsolete("Use the async method instead.")]
        Stream? OpenCommandFile(bool writing);

        #endregion

        #region Visual Effects

        /// <summary>
        /// Changes the current text style.
        /// </summary>
        /// <param name="style">The style being requested.</param>
        /// <remarks>
        /// The interface module may optionally allow styles to be combined; for example, requesting
        /// italic when the bold style is already selected may result in bold italic text, or it may
        /// simply result in italic. In any case, selecting <see cref="TextStyle.Roman"/> must return
        /// to plain text.
        /// </remarks>
        /// <seealso cref="BoldAvailable"/>
        /// <seealso cref="ItalicAvailable"/>
        /// <seealso cref="FixedPitchAvailable"/>
        void SetTextStyle(TextStyle style);
        /// <summary>
        /// Changes the size of the upper window.
        /// </summary>
        /// <param name="lines">The new height of the upper window, in lines, or 0 to
        /// turn off the upper window.</param>
        void SplitWindow(short lines);
        /// <summary>
        /// Selects the upper or lower window.
        /// </summary>
        /// <param name="num">0 to select the lower window, or 1 for the upper window.</param>
        void SelectWindow(short num);
        /// <summary>
        /// Erases one or both windows.
        /// </summary>
        /// <param name="num">0 to erase the lower window, 1 to erase the upper window,
        /// -1 to erase the whole screen and turn off the upper window, or -2 to erase
        /// the whole screen but keep the windows split as they are.</param>
        /// <remarks>
        /// After erasing a window, the cursor should be returned to its upper left corner.
        /// After erasing the entire screen (-1 or -2), the lower window should be selected
        /// and the cursor returned to its upper left corner.
        /// </remarks>
        void EraseWindow(short num);
        /// <summary>
        /// Erases everything to the right of the cursor position on the current line,
        /// leaving the cursor where it is.
        /// </summary>
        void EraseLine();
        /// <summary>
        /// Moves the cursor, if the upper window is selected.
        /// </summary>
        /// <param name="x">The X coordinate of the new cursor position, counting from 1.</param>
        /// <param name="y">The Y coordinate of the new cursor position, counting from 1.</param>
        /// <remarks>
        /// The coordinate system is "screen units", the same system used by <see cref="FontHeight"/>,
        /// <see cref="FontWidth"/>, <see cref="HeightUnits"/>, and <see cref="WidthUnits"/>.
        /// </remarks>
        void MoveCursor(short x, short y);
        /// <summary>
        /// Retrieves the current cursor position, relative to the top of the currently selected
        /// window.
        /// </summary>
        /// <param name="x">Set to the cursor X coordinate, counting from 1.</param>
        /// <param name="y">Set to the cursor Y coordinate, counting from 1.</param>
        /// <remarks>
        /// The coordinate system is "screen units", the same system used by <see cref="FontHeight"/>,
        /// <see cref="FontWidth"/>, <see cref="HeightUnits"/>, and <see cref="WidthUnits"/>.
        /// </remarks>
        void GetCursorPos(out short x, out short y);
        /// <summary>
        /// Sets the current output colors.
        /// </summary>
        /// <param name="fg">The new foreground color.</param>
        /// <param name="bg">The new background color.</param>
        /// <remarks>
        /// The regular color values are: 2 (black), 3 (red), 4 (green), 5 (yellow), 6 (blue),
        /// 7 (magenta), 8 (cyan), 9 (white), 10 (light grey), 11 (medium grey), or 12 (dark grey).
        /// There are also two special color values: 0 means "no change" and 1 means "return to
        /// the default".
        /// </remarks>
        /// <seealso cref="ColorsAvailable"/>
        void SetColors(short fg, short bg);
        /// <summary>
        /// Sets the current output font.
        /// </summary>
        /// <param name="num">The new font number, or 0 to return to the previous font.</param>
        /// <returns>The previous font number, or 0 if the requested font is not available
        /// (and thus the font has not been changed).</returns>
        /// <remarks>
        /// The standard font numbers are 1 (normal font), 2 ("picture font"), 3 (character
        /// graphics font), and 4 (Courier-style fixed pitch font). However, font 2 is not
        /// expected to be supported; its definition is lost to history.
        /// </remarks>
        /// <seealso cref="GraphicsFontAvailable"/>
        short SetFont(short num);

        /// <summary>
        /// Allows the I/O module to substitute its own status line handling for V1-3 games.
        /// </summary>
        /// <param name="location">The name of the player's location.</param>
        /// <param name="hoursOrScore">Time games: the current hour (0-23). Score games: the
        /// player's score.</param>
        /// <param name="minsOrTurns">Time games: the current minute (0-59). Score games: the
        /// number of turns elapsed.</param>
        /// <param name="useTime"><b>true</b> if this is a time game, or <b>false</b> if
        /// this is a score game.</param>
        /// <returns><b>true</b> to indicate that the status line request has been handled,
        /// or <b>false</b> to allow ZLR's default status line handler to print it.</returns>
        bool DrawCustomStatusLine([NotNull] string location, short hoursOrScore, short minsOrTurns, bool useTime);

        #endregion

        #region Sound Effects

        /// <summary>
        /// Plays, stops, or controls the cache status of a sound sample.
        /// </summary>
        /// <param name="number">The number of the sound.</param>
        /// <param name="action">The action being requested of the sound.</param>
        /// <param name="volume">The volume at which to play the sound, from 1 (quiet) to 8
        /// (loud). Values higher than 8 should be treated the same as 8, i.e., loudest.</param>
        /// <param name="repeats">The number of times the sound should be played, or 255
        /// to repeat the sound forever.</param>
        /// <param name="callback">A function to call after the sound is finished playing or
        /// repeating. This should not be called if the sound is explicitly stopped.</param>
        /// <remarks>
        /// Sampled sounds are played in the background: this method must not wait for
        /// the sound to finish before returning.
        /// </remarks>
        /// <seealso cref="SoundSamplesAvailable"/>
        void PlaySoundSample(ushort number, SoundAction action, byte volume, byte repeats,
            [NotNull] SoundFinishedCallback callback);
        /// <summary>
        /// Plays a beep sound.
        /// </summary>
        /// <param name="highPitch">True to play a high pitched beep, or false for a
        /// low pitched beep.</param>
        /// <remarks>
        /// Beep sounds are synchronous, so this method may wait for the beep to finish
        /// before returning.
        /// </remarks>
        void PlayBeep(bool highPitch);

        #endregion

        #region Capabilities

        /// <summary>
        /// Gets or sets a value indicating whether all text will be displayed
        /// in a fixed pitch font.
        /// </summary>
        /// <remarks>
        /// This only affects the lower window, because the upper window is always fixed pitch.
        /// </remarks>
        bool ForceFixedPitch { get; set; }

        /// <summary>
        /// Gets a value indicating whether text is displayed in a variable pitch font by default.
        /// </summary>
        /// <remarks>
        /// This only affects the lower window, because the upper window is always fixed pitch.
        /// </remarks>
        bool VariablePitchAvailable { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the lower window should scroll from the bottom.
        /// </summary>
        bool ScrollFromBottom { get; set; }

        /// <summary>
        /// Gets a value indicating whether the bold text style is available.
        /// </summary>
        /// <seealso cref="SetTextStyle"/>
        bool BoldAvailable { get; }
        /// <summary>
        /// Gets a value indicating whether the italic text style is available.
        /// </summary>
        /// <seealso cref="SetTextStyle"/>
        bool ItalicAvailable { get; }
        /// <summary>
        /// Gets a value indicating whether the fixed pitch text style is available.
        /// </summary>
        /// <seealso cref="SetTextStyle"/>
        bool FixedPitchAvailable { get; }
        /// <summary>
        /// Gets a value indicating whether the character graphics font is available.
        /// </summary>
        /// <seealso cref="SetFont"/>
        bool GraphicsFontAvailable { get; }

        /// <summary>
        /// Gets a value indicating whether timed input is available, i.e., whether
        /// the callback parameter to <see cref="ReadLine"/> and <see cref="ReadKey"/>
        /// will actually be called periodically.
        /// </summary>
        /// <seealso cref="ReadLine"/>
        /// <seealso cref="ReadKey"/>
        [Obsolete("Use async input and a timer task instead.")]
        bool TimedInputAvailable { get; }
        /// <summary>
        /// Gets a value indicating whether sampled sound is available, i.e., whether
        /// <see cref="PlaySoundSample"/> will actually have an effect.
        /// </summary>
        /// <seealso cref="PlaySoundSample"/>
        bool SoundSamplesAvailable { get; }

        /// <summary>
        /// Gets the width of the screen in characters.
        /// </summary>
        /// <remarks>
        /// The standard "character" here is the digit "0" in the fixed pitch font.
        /// </remarks>
        byte WidthChars { get; }
        /// <summary>
        /// Gets the width of the screen in screen units.
        /// </summary>
        /// <remarks>
        /// For simplicity, it is recommended to fix the font size at 1 by 1 so that
        /// characters and screen units are the same.
        /// </remarks>
        short WidthUnits { get; }
        /// <summary>
        /// Gets the height of the screen in characters.
        /// </summary>
        /// <remarks>
        /// The standard "character" here is the digit "0" in the fixed pitch font.
        /// </remarks>
        byte HeightChars { get; }
        /// <summary>
        /// Gets the height of the screen in screen units.
        /// </summary>
        /// <remarks>
        /// For simplicity, it is recommended to fix the font size at 1 by 1 so that
        /// characters and screen units are the same.
        /// </remarks>
        short HeightUnits { get; }
        /// <summary>
        /// Gets the height of a character in screen units.
        /// </summary>
        /// <remarks>
        /// The standard "character" here is the digit "0" in the fixed pitch font.
        /// For simplicity, it is recommended to fix the font size at 1 by 1 so that
        /// characters and screen units are the same.
        /// </remarks>
        byte FontHeight { get; }
        /// <summary>
        /// Gets the width of a character in screen units.
        /// </summary>
        /// <remarks>
        /// The standard "character" here is the digit "0" in the fixed pitch font.
        /// For simplicity, it is recommended to fix the font size at 1 by 1 so that
        /// characters and screen units are the same.
        /// </remarks>
        byte FontWidth { get; }

        /// <summary>
        /// Raised when the screen size has changed.
        /// </summary>
        /// <remarks>
        /// The VM will respond by reading the new size values and writing them into
        /// the game header.
        /// </remarks>
        event EventHandler SizeChanged;

        /// <summary>
        /// Gets a value indicating whether color text is available, i.e., whether
        /// <see cref="SetColors"/> will actually have an effect.
        /// </summary>
        /// <seealso cref="SetColors"/>
        bool ColorsAvailable { get; }
        /// <summary>
        /// Gets the default foreground color.
        /// </summary>
        byte DefaultForeground { get; }
        /// <summary>
        /// Gets the default background color.
        /// </summary>
        byte DefaultBackground { get; }

        /// <summary>
        /// Determines whether a given character can be printed to the screen
        /// or received as input.
        /// </summary>
        /// <param name="ch">The character to test.</param>
        /// <returns>A <see cref="UnicodeCaps"/> value indicating whether the
        /// character can be printed or received.</returns>
        UnicodeCaps CheckUnicode(char ch);

        #endregion
    }

    [PublicAPI]
    public interface IAsyncZMachineIO : IZMachineIO
    {
        /// <summary>
        /// Reads a line of input from the player asynchronously.
        /// </summary>
        /// <param name="initial">The initial string which has been supplied for the player's
        /// input, or an empty string if no initial input has been supplied.</param>
        /// <param name="terminatingKeys">An array of ZSCII values of function keys which should
        /// terminate input immediately if pressed. The special value 255 means "any function key" and will
        /// appear alone.</param>
        /// <param name="allowDebuggerBreak"><b>true</b> if the function may break into the debugger by
        /// returning <see cref="ReadLineResult.DebuggerBreak"/>.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="ReadLineResult"/> indicating how the line input request ended.</returns>
        /// <remarks>
        /// <para>If a non-empty string is supplied as <paramref name="initial"/>, the string will have
        /// already been printed by the game. The interface should avoid printing it again, but should
        /// still allow the player to edit it as if he had typed it himself. (If this cannot be achieved,
        /// it is recommended to err on the side of letting the player edit the text.)</para>
        /// </remarks>
        [NotNull]
        Task<ReadLineResult> ReadLineAsync([NotNull] string initial, byte[] terminatingKeys,
            bool allowDebuggerBreak, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reads a single key of input from the player asynchronously, without echoing it.
        /// </summary>
        /// <param name="translator">A helper callback which translates printable characters into their
        /// ZSCII values, according to the currently selected translation table.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The ZSCII value of the key that was pressed, or 0 if input was cancelled by the
        /// timer callback.</returns>
        [NotNull]
        Task<short> ReadKeyAsync([NotNull] CharTranslator translator, CancellationToken cancellationToken = default);


          /// <summary>
        /// Opens a stream to write the saved game file.
        /// </summary>
        /// <param name="size">The size of the game state that will be written, in bytes.</param>
        /// <returns>A writable <see cref="System.IO.Stream"/> for the save file, which the
        /// VM will close after it's done saving; or <see langword="null"/> if the user chose not to select a
        /// file or the file couldn't be opened.</returns>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <remarks>
        /// The interface module is responsible for prompting the player for a file name, if necessary.
        /// </remarks>
        [NotNull]
        [ItemCanBeNull]
        Task<Stream?> OpenSaveFileAsync(int size, CancellationToken cancellationToken = default);
        /// <summary>
        /// Opens a stream to read a previously saved game file.
        /// </summary>
        /// <returns>A readable <see cref="System.IO.Stream"/> for the save file, which the
        /// VM will close after it's done loading; or <see langword="null"/> if the user chose not to select a
        /// file or the file couldn't be opened.</returns>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <remarks>
        /// The interface module is responsible for prompting the player for a file name, if necessary.
        /// </remarks>
        [NotNull]
        [ItemCanBeNull]
        Task<Stream?> OpenRestoreFileAsync(CancellationToken cancellationToken = default);
        /// <summary>
        /// Opens a stream to read or write auxiliary game data.
        /// </summary>
        /// <param name="name">A suggested name for the auxiliary file.</param>
        /// <param name="size">The size, in bytes, of the array that will be read from or
        /// written to the auxiliary file.</param>
        /// <param name="writing">True if the stream will be used to save auxiliary data;
        /// false if it will be used to read previously saved data.</param>
        /// <returns>A <see cref="System.IO.Stream"/> for the auxiliary file, which must be
        /// readable or writable depending on the value of <paramref name="writing"/>, and
        /// which the VM will close after it's done using; or <see langword="null"/> if the user chose not to
        /// select a file or the file couldn't be opened.</returns>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <remarks>
        /// The interface module is responsible for prompting the player for a file name, if necessary.
        /// The interface module may choose to use the suggested name as-is, or prompt the user
        /// for a name and use the suggested name as a default. The suggested name should at least
        /// be visible to the user, since a game may use several auxiliary files.
        /// </remarks>
        [NotNull]
        [ItemCanBeNull]
        Task<Stream?> OpenAuxiliaryFileAsync([NotNull] string name, int size, bool writing, CancellationToken cancellationToken = default);
        /// <summary>
        /// Opens a stream to read or write the player's input to a file.
        /// </summary>
        /// <param name="writing">True if the stream will be used to record the player's
        /// input; false if it will be used to replay previously recorded input.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A <see cref="System.IO.Stream"/> for the command file, which must be
        /// readable or writable depending on the value of <paramref name="writing"/>, and
        /// which the VM will close after it's done using; or <see langword="null"/> if the user chose not to
        /// select a file or the file couldn't be opened.</returns>
        [NotNull]
        [ItemCanBeNull]
        Task<Stream?> OpenCommandFileAsync(bool writing, CancellationToken cancellationToken = default);
    }

    class AsyncZMachineIOAdapter : IAsyncZMachineIO
    {
        [NotNull] private readonly IZMachineIO next;

        public AsyncZMachineIOAdapter([NotNull] IZMachineIO next)
        {
            this.next = next;
        }

        [Obsolete]
        public ReadLineResult ReadLine(string initial, int time, TimedInputCallback callback, byte[] terminatingKeys,
            bool allowDebuggerBreak) =>
            next.ReadLine(initial, time, callback, terminatingKeys, allowDebuggerBreak);

        [Obsolete]
        public short ReadKey(int time, TimedInputCallback callback, CharTranslator translator) => next.ReadKey(time, callback, translator);

        public void PutCommand(string command) => next.PutCommand(command);

        public void PutChar(char ch) => next.PutChar(ch);

        public void PutString(string str) => next.PutString(str);

        public void PutTextRectangle(string[] lines) => next.PutTextRectangle(lines);

        public bool Buffering
        {
            get => next.Buffering;
            set => next.Buffering = value;
        }

        public bool Transcripting
        {
            get => next.Transcripting;
            set => next.Transcripting = value;
        }

        public void PutTranscriptChar(char ch) => next.PutTranscriptChar(ch);

        public void PutTranscriptString(string str) => next.PutTranscriptString(str);

        [Obsolete]
        public Stream? OpenSaveFile(int size) => next.OpenSaveFile(size);

        [Obsolete]
        public Stream? OpenRestoreFile() => next.OpenRestoreFile();

        [Obsolete]
        public Stream? OpenAuxiliaryFile(string name, int size, bool writing) => next.OpenAuxiliaryFile(name, size, writing);

        [Obsolete]
        public Stream? OpenCommandFile(bool writing) => next.OpenCommandFile(writing);

        public void SetTextStyle(TextStyle style) => next.SetTextStyle(style);

        public void SplitWindow(short lines) => next.SplitWindow(lines);

        public void SelectWindow(short num) => next.SelectWindow(num);

        public void EraseWindow(short num) => next.EraseWindow(num);

        public void EraseLine() => next.EraseLine();

        public void MoveCursor(short x, short y) => next.MoveCursor(x, y);

        public void GetCursorPos(out short x, out short y) => next.GetCursorPos(out x, out y);

        public void SetColors(short fg, short bg) => next.SetColors(fg, bg);

        public short SetFont(short num) => next.SetFont(num);

        public bool DrawCustomStatusLine(string location, short hoursOrScore, short minsOrTurns, bool useTime) => next.DrawCustomStatusLine(location, hoursOrScore, minsOrTurns, useTime);

        public void PlaySoundSample(ushort number, SoundAction action, byte volume, byte repeats, SoundFinishedCallback callback)
        {
            next.PlaySoundSample(number, action, volume, repeats, callback);
        }

        public void PlayBeep(bool highPitch)
        {
            next.PlayBeep(highPitch);
        }

        public bool ForceFixedPitch
        {
            get => next.ForceFixedPitch;
            set => next.ForceFixedPitch = value;
        }

        public bool VariablePitchAvailable => next.VariablePitchAvailable;

        public bool ScrollFromBottom
        {
            get => next.ScrollFromBottom;
            set => next.ScrollFromBottom = value;
        }

        public bool BoldAvailable => next.BoldAvailable;

        public bool ItalicAvailable => next.ItalicAvailable;

        public bool FixedPitchAvailable => next.FixedPitchAvailable;

        public bool GraphicsFontAvailable => next.GraphicsFontAvailable;

        [Obsolete]
        public bool TimedInputAvailable => next.TimedInputAvailable;

        public bool SoundSamplesAvailable => next.SoundSamplesAvailable;

        public byte WidthChars => next.WidthChars;

        public short WidthUnits => next.WidthUnits;

        public byte HeightChars => next.HeightChars;

        public short HeightUnits => next.HeightUnits;

        public byte FontHeight => next.FontHeight;

        public byte FontWidth => next.FontWidth;

        public event EventHandler SizeChanged
        {
            add => next.SizeChanged += value;
            remove => next.SizeChanged -= value;
        }

        public bool ColorsAvailable => next.ColorsAvailable;

        public byte DefaultForeground => next.DefaultForeground;

        public byte DefaultBackground => next.DefaultBackground;

        public UnicodeCaps CheckUnicode(char ch) => next.CheckUnicode(ch);

        #region Async Adapters
#pragma warning disable 618

        public async Task<ReadLineResult> ReadLineAsync(string initial, byte[] terminatingKeys, bool allowDebuggerBreak, CancellationToken cancellationToken)
        {
            /**
             * This method needs to be cancelable via the token, even if
             * <see cref="IZMachineIO.ReadLine(string, int, TimedInputCallback, byte[], bool)"/> isn't cooperative.
             */

            const int TIMED_INPUT_TIMEOUT_TENTHS = 1; // check token this often if timed input is available
            const int GRACE_PERIOD_MS = 500;          // give the callback this long to cancel the task

            cancellationToken.ThrowIfCancellationRequested();

            /**
             * If <see cref="next"/> doesn't implement timed input, we just stop waiting. The read will likely
             * continue in the background, but we can't help that; our top priority is canceling this task.
             */

            if (!cancellationToken.CanBeCanceled || !next.TimedInputAvailable)
            {
                var untimedReadTask = Task.Factory.StartNew(
                    () =>
                    {
                        var line = next.ReadLine(initial, 0, () => false, terminatingKeys, allowDebuggerBreak);
                        Debug.WriteLine("[async][untimed] Line read: {0}", line);
                        return line;
                    },
                    cancellationToken,
                    TaskCreationOptions.DenyChildAttach | TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
                return await untimedReadTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            /**
             * If <see cref="next"/> does implement timed input, we pass a timeout and callback to check
             * the token and cancel gracefully. We might still give up and let it continue in the background
             * if that doesn't work, but we give it some time first.
             */

            bool CheckToken()
            {
                cancellationToken.ThrowIfCancellationRequested();
                return false;
            }

            var timedReadTask = Task.Run(
                () =>
                {
                    var line = next.ReadLine(initial, TIMED_INPUT_TIMEOUT_TENTHS, CheckToken, terminatingKeys, allowDebuggerBreak);
                    Debug.WriteLine("[async][timed] Line read: {0}", line);
                    return line;
                },
                cancellationToken);

            try
            {
                return await timedReadTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken && timedReadTask.Status == TaskStatus.Running)
            {
                var graceCts = new CancellationTokenSource(GRACE_PERIOD_MS);
                return await timedReadTask.WaitAsync(graceCts.Token).ConfigureAwait(false);
            }
        }

        public Task<short> ReadKeyAsync(CharTranslator translator, CancellationToken cancellationToken)
        {
            bool Callback()
            {
                cancellationToken.ThrowIfCancellationRequested();
                return false;
            }

            return Task.Run(() => next.ReadKey(0, Callback, translator), cancellationToken);
        }

        public Task<Stream?> OpenSaveFileAsync(int size, CancellationToken cancellationToken)
        {
            return Task.Run(() => next.OpenSaveFile(size), cancellationToken);
        }

        public Task<Stream?> OpenRestoreFileAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() => next.OpenRestoreFile(), cancellationToken);
        }

        public Task<Stream?> OpenAuxiliaryFileAsync(string name, int size, bool writing, CancellationToken cancellationToken)
        {
            return Task.Run(() => next.OpenAuxiliaryFile(name, size, writing), cancellationToken);
        }

        public Task<Stream?> OpenCommandFileAsync(bool writing, CancellationToken cancellationToken)
        {
            return Task.Run(() => next.OpenCommandFile(writing), cancellationToken);
        }

#pragma warning restore 618
        #endregion

        [NotNull]
        [Obsolete("Implement IAsyncZMachineIO directly.")]
        public static IAsyncZMachineIO Wrap([NotNull] IZMachineIO io) =>
            io as IAsyncZMachineIO ??
            new AsyncZMachineIOAdapter(io ?? throw new ArgumentNullException(nameof(io)));
    }

    partial class ZMachine
    {
        private int DictWordSizeInZchars => zversion >= 4 ? 9 : 6;
        private int DictWordSizeInBytes => DictWordSizeInZchars * 2 / 3;

#pragma warning disable 0169
        internal void PrintZSCII(short zc)
        {
            if (zc == 0)
                return;

            if (TableOutputEnabled)
            {
                var (_, buffer) = tableOutputStack.Peek();
                buffer.Add((byte)zc);
            }
            else
            {
                var ch = CharFromZSCII(zc);
                if (normalOutput)
                    io.PutChar(ch);
                if (io.Transcripting)
                    io.PutTranscriptChar(ch);
            }
        }

        internal void PrintUnicode(ushort uc)
        {
            if (TableOutputEnabled)
            {
                var (_, buffer) = tableOutputStack.Peek();
                buffer.Add((byte)CharToZSCII((char)uc));
            }
            else
            {
                if (normalOutput)
                    io.PutChar((char)uc);
                if (io.Transcripting)
                    io.PutTranscriptChar((char)uc);
            }
        }

        internal void PrintString([NotNull] string str)
        {
            if (this.TableOutputEnabled)
            {
                var (_, buffer) = tableOutputStack.Peek();
                foreach (var ch in str)
                    buffer.Add((byte)CharToZSCII(ch));
            }
            else
            {
                if (normalOutput)
                    io.PutString(str);
                if (io.Transcripting)
                    io.PutTranscriptString(str);
            }
        }
#pragma warning restore 0169

        private char CharFromZSCII(short ch)
        {
            switch (ch)
            {
                case 13:
                    return '\n';

                default:
                    if (ch >= 155 && ch < 155 + extraChars.Length)
                        return extraChars[ch - 155];
                    else
                        return (char)ch;
            }
        }

        private short CharToZSCII(char ch)
        {
            switch (ch)
            {
                case '\n':
                    return 13;

                default:
                    var idx = Array.IndexOf(extraChars, ch);
                    if (idx >= 0)
                        return (short)(155 + idx);
                    else
                        return (short)ch;
            }
        }

        [NotNull]
        private byte[] StringToZSCII([NotNull] string str)
        {
            var result = new byte[str.Length];
            for (var i = 0; i < str.Length; i++)
                result[i] = (byte)CharToZSCII(str[i]);
            return result;
        }

        // default alphabets (S 3.5.3)
        [NotNull]
        private static readonly char[] DefaultAlphabet0 =
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
            'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
        };

        [NotNull]
        private static readonly char[] DefaultAlphabet1 =
        {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
            'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
        };

        [NotNull]
        private static readonly char[] DefaultAlphabet2 =
        {
            ' ', '\n', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.',
            ',', '!', '?', '_', '#', '\'', '"', '/', '\\', '-', ':', '(', ')',
        };

        // default Unicode translations (S 3.8.5.3)
        private static readonly char[] DefaultExtraChars =
        {
            '\u00e4', '\u00f6', '\u00fc', '\u00c4', '\u00d6', '\u00dc', '\u00df', '\u00bb', '\u00ab', '\u00eb', // 155
            '\u00ef', '\u00ff', '\u00cb', '\u00cf', '\u00e1', '\u00e9', '\u00ed', '\u00f3', '\u00fa', '\u00fd', // 165
            '\u00c1', '\u00c9', '\u00cd', '\u00d3', '\u00da', '\u00dd', '\u00e0', '\u00e8', '\u00ec', '\u00f2', // 175
            '\u00f9', '\u00c0', '\u00c8', '\u00cc', '\u00d2', '\u00d9', '\u00e2', '\u00ea', '\u00ee', '\u00f4', // 185
            '\u00fb', '\u00c2', '\u00ca', '\u00ce', '\u00d4', '\u00db', '\u00e5', '\u00c5', '\u00f8', '\u00d8', // 195
            '\u00e3', '\u00f1', '\u00f5', '\u00c3', '\u00d1', '\u00d5', '\u00e6', '\u00c6', '\u00e7', '\u00c7', // 205
            '\u00fe', '\u00f0', '\u00de', '\u00d0', '\u00a3', '\u0153', '\u0152', '\u00a1', '\u00bf'            // 215
        };

        [NotNull]
        internal string DecodeString(int address) => DecodeStringWithLen(address, out _);

        [NotNull]
        private string DecodeStringWithLen(int address, out int len)
        {
            len = 0;

            var alphabet = 0;
            var abbrevMode = 0;
            short word;
            var sb = new StringBuilder();

            do
            {
                word = GetWord(address);
                address += 2;
                len += 2;

                DecodeChar((word >> 10) & 0x1F, ref alphabet, ref abbrevMode, sb);
                DecodeChar((word >> 5) & 0x1F, ref alphabet, ref abbrevMode, sb);
                DecodeChar(word & 0x1F, ref alphabet, ref abbrevMode, sb);
            } while ((word & 0x8000) == 0);

            return sb.ToString();
        }

        private void DecodeChar(int zchar, ref int alphabet, ref int abbrevMode, [NotNull] StringBuilder sb)
        {
            switch (abbrevMode)
            {
                case 1:
                case 2:
                case 3:
                    sb.Append(GetAbbreviation((short)(32 * (abbrevMode - 1) + zchar)));
                    abbrevMode = 0;
                    return;

                case 4:
                    abbrevMode = 5;
                    alphabet = zchar;
                    return;
                case 5:
                    abbrevMode = 0;
                    sb.Append(CharFromZSCII((short)((alphabet << 5) + zchar)));
                    alphabet = 0;
                    return;
            }

            switch (zchar)
            {
                case 0:
                    sb.Append(' ');
                    return;

                case 1:
                case 2:
                case 3:
                    abbrevMode = zchar;
                    return;

                case 4:
                    alphabet = 1;
                    return;
                case 5:
                    alphabet = 2;
                    return;
            }

            zchar -= 6;
            switch (alphabet)
            {
                case 0:
                    sb.Append(alphabet0[zchar]);
                    return;

                case 1:
                    sb.Append(alphabet1[zchar]);
                    alphabet = 0;
                    return;

                case 2:
                    if (zchar == 0)
                        abbrevMode = 4;
                    else
                        sb.Append(alphabet2[zchar]);
                    alphabet = 0;
                    return;
            }
        }

        [NotNull]
        private string GetAbbreviation(int num)
        {
            var address = (ushort)GetWord(abbrevTable + num * 2);
            return DecodeString(address * 2); // word address, not byte address!
        }

        private void HandleSoundFinished(ushort routine)
        {
            EnterFunctionImpl((short)routine, null, 0, pc);
            JitLoopAsync().Wait(interruptToken);  //XXX asyncify
        }

        internal async Task SetOutputStreamAsync(short num, ushort address, int nextPC)
        {
            var enabled = true;
            if (num < 0)
            {
                num = (short)-num;
                enabled = false;
            }

            switch (num)
            {
                case 1:
                    // normal
                    normalOutput = enabled;
                    break;

                case 2:
                    // transcript
                    io.Transcripting = enabled;
                    break;

                case 3:
                    // memory (nestable up to 16 levels)
                    if (enabled)
                    {
                        if (tableOutputStack.Count == 16)
                            throw new Exception("Output stream 3 nested too deeply");
                        if (address < 64 || address + 1 >= RomStart)
                            throw new Exception("Output stream 3 address is out of range");

                        tableOutputStack.Push((address, new List<byte>()));
                    }
                    else if (this.TableOutputEnabled)
                    {
                        var (prevAddress, buffer) = tableOutputStack.Pop();

                        var len = Math.Min(buffer.Count, RomStart - prevAddress - 2);
                        SetWord(prevAddress, (short)len);
                        for (var i = 0; i < len; i++)
                            SetByte(prevAddress + 2 + i, buffer[i]);
                    }
                    break;

                case 4:
                    // player's commands
                    if (enabled)
                    {
                        var cmdStream = await io.OpenCommandFileAsync(true, interruptToken).ConfigureAwait(false);
                        if (cmdStream != null)
                        {
                            cmdWtr?.Dispose();

                            try
                            {
                                cmdWtr = new CommandFileWriter(cmdStream);
                            }
                            catch
                            {
                                cmdWtr = null;
                            }
                        }
                    }
                    else
                    {
                        if (cmdWtr != null)
                        {
                            cmdWtr.Dispose();
                            cmdWtr = null;
                        }
                    }
                    break;

                default:
                    throw new Exception("Invalid output stream #" + num);
            }

            pc = nextPC;
        }

#pragma warning disable 0169
        internal void GetCursorPos(ushort address)
        {
            io.GetCursorPos(out var x, out var y);
            SetWordChecked(address, y);
            SetWordChecked(address + 2, x);
        }
#pragma warning restore 0169
    }
}
