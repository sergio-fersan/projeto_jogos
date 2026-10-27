namespace SilencioBiblioteca
{
    [System.Serializable]
    public struct DialogueLine
    {
        public string who;
        public string text;

        public DialogueLine(string who, string text)
        {
            this.who = who;
            this.text = text;
        }
    }

    public static class L
    {
        public static DialogueLine[] Lines(params DialogueLine[] lines) { return lines; }
        public static DialogueLine D(string who, string text) { return new DialogueLine(who, text); }
        public static DialogueLine N(string text) { return new DialogueLine("", text); }
    }
}
