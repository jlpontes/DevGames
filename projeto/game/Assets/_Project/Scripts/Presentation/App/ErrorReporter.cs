using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ComboTactics.UI
{
    /// <summary>
    /// Tratamento de erro centralizado (SYSTEM-DESIGN §3.5 e §4.4): guarda as últimas linhas do log e
    /// mostra um diálogo com as informações de debug quando algo inesperado acontece.
    /// </summary>
    public sealed class ErrorReporter
    {
        private const int MaxLines = 200;
        private readonly Queue<string> _lines = new Queue<string>();
        private readonly Action<string, string> _showDialog;
        private bool _showing;

        /// <summary>Contexto da tela atual (por exemplo, seed e comandos da batalha) anexado ao relatório.</summary>
        public Func<string> Context;

        public ErrorReporter(Action<string, string> showDialog)
        {
            _showDialog = showDialog;
            Application.logMessageReceived += OnLog;
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLog;
        }

        public void DialogClosed() { _showing = false; }

        public void Report(Exception exception, string where)
        {
            Debug.LogError("[" + where + "] " + exception);
            Show("Algo deu errado em: " + where + ".\n" + exception.Message);
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            _lines.Enqueue("[" + type + "] " + condition + (type == LogType.Exception ? "\n" + stackTrace : ""));
            while (_lines.Count > MaxLines) _lines.Dequeue();
            if (type == LogType.Exception) Show("Ocorreu um erro inesperado.\n" + condition);
        }

        private void Show(string message)
        {
            if (_showing) return;
            _showing = true;
            _showDialog(message, DebugInfo());
        }

        public string DebugInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Combo Tactics " + Application.version + " · Unity " + Application.unityVersion + " · " + Application.platform);
            if (Context != null)
            {
                try { sb.AppendLine(Context()); } catch (Exception) { }
            }
            sb.AppendLine("--- log ---");
            foreach (var line in _lines) sb.AppendLine(line);
            return sb.ToString();
        }
    }
}
