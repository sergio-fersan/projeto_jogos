namespace SilencioBiblioteca
{
    [System.Serializable]
    public struct DialogueLine
    {
        public string who;   // nome mostrado (vazio = narração)
        public string face;  // nome do retrato em Resources/Art/Chars (ex.: "marta_p0"); vazio = sem retrato
        public string text;

        public DialogueLine(string who, string face, string text)
        {
            this.who = who; this.face = face; this.text = text;
        }
    }

    public static class L
    {
        public static DialogueLine[] Lines(params DialogueLine[] lines) { return lines; }
        public static DialogueLine D(string who, string face, string text) { return new DialogueLine(who, face, text); }
        public static DialogueLine Me(string text) { return new DialogueLine("Você", "player_p0", text); }
        public static DialogueLine N(string text) { return new DialogueLine("", "", text); }
    }
}
