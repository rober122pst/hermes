#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using HERMESLANG.Runtime.Errors;
using UnityEngine;
using HermesVm = HERMESLANG.Interpreter.Interpreter;

internal static partial class HermesPatches
{
    private const string RecordPulse =
        "    gravar_pulso(sinal)\n" +
        "senao:\n" +
        "    descartar_ruido()";

    private const string StoreReadings =
        "senao:\n" +
        "    guardar_leituras()";

    private sealed class Pulse
    {
        public readonly int Channel;
        public readonly float Phase;

        public Pulse(int channel, float phase)
        {
            Channel = channel;
            Phase = phase;
        }
    }

    public static HermesPatch RecoverPulse(Vector3 destination, Action<Vector3> publish)
    {
        int variant = DrawVariant("Antena", 3);
        double minimum = PuzzleRandom.Next(52, 69);
        switch (variant)
        {
            case 0:
                return AntennaPatch(
                    "FILTRO DE RECEPÇÃO",
                    $"A H.E.R.M.E.S. descartou o pulso do Cinturão. Grave toda leitura a partir de {minimum:0}%, inclusive no limite.",
                    "sinal = ler_intensidade()\n" +
                    "se sinal {0} sensibilidade_minima():\n" + RecordPulse,
                    new[] { new[] { ">", ">=", "<=" } },
                    new[] { minimum - 8, minimum, minimum + 11 },
                    vm => Read(vm, "sensibilidade_minima", minimum),
                    reading => reading >= minimum, destination, publish);
            case 1:
                double maximum = minimum + 19;
                return AntennaPatch(
                    "SATURAÇÃO DO RECEPTOR",
                    $"Ruído fraco e picos fortes escondem o pulso. A antena deve gravar apenas sinais entre {minimum:0}% e {maximum:0}%, incluindo os dois limites.",
                    "sinal = ler_intensidade()\n" +
                    "se sinal >= faixa_baixa() {0} sinal {1} faixa_alta():\n" + RecordPulse,
                    new[] { new[] { "ou", "e" }, new[] { "<", "<=", ">=" } },
                    new[] { minimum - 5, minimum, maximum, maximum + 6 },
                    vm =>
                    {
                        Read(vm, "faixa_baixa", minimum);
                        Read(vm, "faixa_alta", maximum);
                    },
                    reading => reading >= minimum && reading <= maximum, destination, publish);
            default:
                int gainLost = PuzzleRandom.Next(5, 10);
                return AntennaPatch(
                    "GANHO DA ANTENA",
                    $"A antena mede {gainLost} pontos abaixo do valor real. Recupere o ganho perdido e grave sinais corrigidos de {minimum:0}% ou mais.",
                    "sinal = ler_intensidade() {0} ganho_perdido()\n" +
                    "se sinal {1} sensibilidade_minima():\n" + RecordPulse,
                    new[] { new[] { "-", "+", "*" }, new[] { ">", ">=", "<=" } },
                    new[] { minimum - gainLost - 6, minimum - gainLost, minimum - gainLost + 9 },
                    vm =>
                    {
                        Read(vm, "ganho_perdido", (double)gainLost);
                        Read(vm, "sensibilidade_minima", minimum);
                    },
                    reading => reading >= minimum, destination, publish, gainLost);
        }
    }

    private static HermesPatch AntennaPatch(string title, string brief, string source, string[][] fragments,
        double[] intensities, Action<HermesVm> configure, Func<double, bool> keep,
        Vector3 destination, Action<Vector3> publish, int correction = 0)
    {
        return new HermesPatch(title, brief, source, fragments, program =>
        {
            var trace = new StringBuilder();
            bool valid = true;
            Vector3? coordinates = null;
            foreach (double intensity in intensities)
            {
                double expectedReading = intensity + correction;
                double? recorded = null;
                var vm = new HermesVm();
                Read(vm, "ler_intensidade", intensity);
                configure(vm);
                Register(vm, "gravar_pulso", 1, args =>
                {
                    if (args[0] is not double reading)
                        throw new InvalidTypeException("O gravador precisa de uma intensidade numérica.");
                    recorded = reading;
                    return null;
                });
                Signal(vm, "descartar_ruido", () => recorded = null);
                vm.Interpret(program);

                bool passed = keep(expectedReading) ? recorded == expectedReading : !recorded.HasValue;
                valid &= passed;
                if (passed && recorded.HasValue) coordinates = destination;
                string label = correction == 0 ? $"{intensity:0}% de sinal" : $"{intensity:0}% medidos · {expectedReading:0}% reais";
                string result = recorded.HasValue ? $"pulso gravado a {recorded:0}%" : "leitura descartada";
                Append(trace, passed, label, result);
            }

            return new HermesDiagnostic(trace.ToString(),
                valid && coordinates is Vector3 route ? () => publish(route) : null);
        });
    }

    public static HermesPatch TriangulateRoute(Vector3 origin, Vector3 destination, Action<Vector3> publish)
    {
        int variant = DrawVariant("Triangulacao", 3);
        return variant switch
        {
            0 => TriangulationPatch(
                "JANELA DE TRANSMISSÃO",
                "Combine os pulsos dos três receptores. A rota só pode ser transmitida quando todos os dados estiverem disponíveis e a tempestade der trégua.",
                "se contar_amostras() >= 3 {0} janela_estavel():\n" +
                "    pulso_a = ler_pulso(1)\n" +
                "    pulso_b = ler_pulso(2)\n" +
                "    pulso_c = ler_pulso(3)\n" +
                "    fixacao = triangular(pulso_a, pulso_b, {1})\n" +
                "    transmitir_rota(fixacao)\n" + StoreReadings,
                new[] { new[] { "ou", "e" }, new[] { "pulso_b", "pulso_a", "pulso_c" } },
                origin, destination, (1, 2, 3), publish),
            1 => TriangulationPatch(
                "CANAIS CRUZADOS",
                "Os cabos foram religados fora de ordem. Passe os pulsos na ordem dos eixos X, Y e Z. O receptor 3 carrega X, o receptor 1 carrega Y e o receptor 2 carrega Z.",
                "se contar_amostras() >= 3 e janela_estavel():\n" +
                "    pulso_a = ler_pulso(1)\n" +
                "    pulso_b = ler_pulso(2)\n" +
                "    pulso_c = ler_pulso(3)\n" +
                "    fixacao = triangular({0}, pulso_a, {1})\n" +
                "    transmitir_rota(fixacao)\n" + StoreReadings,
                new[] { new[] { "pulso_a", "pulso_b", "pulso_c" }, new[] { "pulso_a", "pulso_b", "pulso_c" } },
                origin, destination, (3, 1, 2), publish),
            _ => TriangulationPatch(
                "VARREDURA DOS RECEPTORES",
                "A varredura perdeu um receptor. Na janela estável, comece pelo canal 1 e percorra os três canais em ordem, sem pular nenhum.",
                "canal = 1\n" +
                "se contar_amostras() >= 3 e janela_estavel():\n" +
                "    enquanto canal {0} 3:\n" +
                "        coletar(ler_pulso(canal))\n" +
                "        canal = canal + {1}\n" +
                "    transmitir_rota(triangular_coleta())\n" + StoreReadings,
                new[] { new[] { "<", "<=", ">" }, new[] { "2", "1", "3" } },
                origin, destination, (1, 2, 3), publish)
        };
    }

    private static HermesPatch TriangulationPatch(string title, string brief, string source, string[][] fragments,
        Vector3 origin, Vector3 destination, (int X, int Y, int Z) channels, Action<Vector3> publish)
    {
        // Cada receptor carrega uma componente no referencial do satélite.
        Quaternion frame = Quaternion.Euler(21f, 37f, -13f);
        Vector3 phases = Quaternion.Inverse(frame) * (destination - origin);
        var pulses = new Pulse[3];
        pulses[channels.X - 1] = new Pulse(channels.X, phases.x);
        pulses[channels.Y - 1] = new Pulse(channels.Y, phases.y);
        pulses[channels.Z - 1] = new Pulse(channels.Z, phases.z);

        Vector3? Triangulate(object? x, object? y, object? z)
        {
            if (x is not Pulse a || a.Channel != channels.X ||
                y is not Pulse b || b.Channel != channels.Y ||
                z is not Pulse c || c.Channel != channels.Z)
                return null;

            return origin + frame * new Vector3(a.Phase, b.Phase, c.Phase);
        }

        return new HermesPatch(title, brief, source, fragments, program =>
        {
            var trace = new StringBuilder();
            bool valid = true;
            Vector3? coordinates = null;
            foreach (var (count, calm) in new[] { (2, true), (3, false), (3, true) })
            {
                bool attempted = false;
                Vector3? published = null;
                var collection = new List<Pulse>(3);
                var vm = new HermesVm();
                Read(vm, "contar_amostras", (double)count);
                Read(vm, "janela_estavel", calm);
                Register(vm, "ler_pulso", 1, args =>
                {
                    int receiver = args[0] is double index ? (int)index : 0;
                    return receiver >= 1 && receiver <= count ? pulses[receiver - 1] : null;
                });
                Register(vm, "triangular", 3, args => Triangulate(args[0], args[1], args[2]));
                Register(vm, "coletar", 1, args =>
                {
                    if (args[0] is Pulse pulse) collection.Add(pulse);
                    return null;
                });
                Register(vm, "triangular_coleta", 0, _ => collection.Count == 3
                    ? Triangulate(collection[0], collection[1], collection[2])
                    : null);
                Register(vm, "transmitir_rota", 1, args =>
                {
                    attempted = true;
                    if (count >= 3 && calm && args[0] is Vector3 route) published = route;
                    return null;
                });
                Signal(vm, "guardar_leituras", () => { });
                vm.Interpret(program);

                bool canTransmit = count >= 3 && calm;
                bool passed = canTransmit ? published.HasValue : !attempted;
                valid &= passed;
                string result = !attempted ? "leituras guardadas" : published.HasValue ? "rota triangulada" :
                    count < 3 ? "transmissão com dados incompletos" :
                    !calm ? "transmissão durante a tempestade" : "fixação inválida. Revise a ordem e os canais";
                Append(trace, passed, $"{count} pulsos · {(calm ? "janela estável" : "tempestade")}", result);
                if (canTransmit) coordinates = published;
            }

            return new HermesDiagnostic(trace.ToString(),
                valid && coordinates is Vector3 route ? () => publish(route) : null);
        });
    }
}
