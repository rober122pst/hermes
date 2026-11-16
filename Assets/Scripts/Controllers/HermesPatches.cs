#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using HERMESLANG.Parser;
using HERMESLANG.Runtime;
using UnityEngine;
using HermesVm = HERMESLANG.Interpreter.Interpreter;

internal sealed class HermesPatch
{
    public readonly string Title;
    public readonly string Brief;
    public readonly string Source;
    public readonly string[][] Fragments;
    public readonly int[] InitialSelections;
    public readonly Func<List<Statement>, HermesDiagnostic> Verify;

    public HermesPatch(string title, string brief, string source, string[][] fragments,
        Func<List<Statement>, HermesDiagnostic> verify)
    {
        Title = title;
        Brief = brief;
        Source = source;
        Verify = verify;
        Fragments = new string[fragments.Length][];
        InitialSelections = new int[fragments.Length];

        for (int slot = 0; slot < fragments.Length; slot++)
        {
            // O primeiro fragmento de cada definição faz parte da rotina defeituosa.
            string[] options = (string[])fragments[slot].Clone();
            HermesPatches.ShuffleOptions(options);
            Fragments[slot] = options;
            InitialSelections[slot] = Array.IndexOf(options, fragments[slot][0]);
        }
    }
}

internal readonly struct HermesDiagnostic
{
    public readonly string Trace;
    public readonly Action? Apply;

    public HermesDiagnostic(string trace, Action? apply)
    {
        Trace = trace;
        Apply = apply;
    }
}

internal static partial class HermesPatches
{
    private static readonly System.Random PuzzleRandom = new();

    private sealed class Command : IHermesFunction
    {
        private readonly Func<List<object?>, object?> execute;
        public string Name { get; }
        public int Arity { get; }

        public Command(string name, int arity, Func<List<object?>, object?> execute)
        {
            Name = name;
            Arity = arity;
            this.execute = execute;
        }

        public object? Execute(List<object?> arguments) => execute(arguments);
    }

    internal static void ShuffleOptions(string[] options)
    {
        for (int i = options.Length - 1; i > 0; i--)
        {
            int other = PuzzleRandom.Next(i + 1);
            (options[i], options[other]) = (options[other], options[i]);
        }
    }

    private static int DrawVariant(string family, int count)
    {
        string key = "HERMES.Puzzles." + family;
        int all = (1 << count) - 1;
        int remaining = PlayerPrefs.GetInt(key + ".Remaining", all) & all;
        if (remaining == 0) remaining = all;

        // Mesmo na virada do ciclo, o último desafio não sai de novo em seguida.
        int last = PlayerPrefs.GetInt(key + ".Last", -1);
        var candidates = new List<int>(count);
        for (int i = 0; i < count; i++)
        {
            if ((remaining & (1 << i)) != 0 && i != last) candidates.Add(i);
        }

        int chosen = candidates.Count > 0 ? candidates[PuzzleRandom.Next(candidates.Count)] : last;
        PlayerPrefs.SetInt(key + ".Remaining", remaining & ~(1 << chosen));
        PlayerPrefs.SetInt(key + ".Last", chosen);
        PlayerPrefs.Save();
        return chosen;
    }

    private static void Append(StringBuilder trace, bool passed, string telemetry, string result)
    {
        if (trace.Length > 0) trace.Append("\n\n");
        trace.Append(passed ? "OK  " : "FALHA  ").Append(telemetry).Append('\n').Append("     ").Append(result);
    }

    private static void Read(HermesVm vm, string name, object value)
    {
        Register(vm, name, 0, _ => value);
    }

    private static void Signal(HermesVm vm, string name, Action action)
    {
        Register(vm, name, 0, _ =>
        {
            action();
            return null;
        });
    }

    private static void Register(HermesVm vm, string name, int arity, Func<List<object?>, object?> execute)
    {
        vm.RegisterFunction(new Command(name, arity, execute));
    }
}
