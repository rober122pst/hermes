#nullable enable
using System;
using System.Text;
using HERMESLANG.Runtime.Errors;
using HermesVm = HERMESLANG.Interpreter.Interpreter;

internal static partial class HermesPatches
{
    private const string InstallModule =
        "    acoplar_modulo()\n" +
        "senao:\n" +
        "    aguardar_calibracao()";

    public static HermesPatch RewriteEvacuation(Action apply)
    {
        int variant = DrawVariant("Evacuacao", 3);
        return variant switch
        {
            0 => EvacuationPatch(
                "BLOQUEIO DE EMERGÊNCIA",
                "O protocolo ainda exclui famílias. Selecione a lista pública e sele o corredor somente se houver risco confirmado e a evacuação for injusta.",
                "se risco_confirmado() {0} nao evacuacao_justa():\n" +
                "    selar_corredor()\n" +
                "senao:\n" +
                "    abrir_corredor({1})",
                new[] { new[] { "ou", "e" }, new[] { "lista_reservada()", "lista_publica()" } }, apply),
            1 => EvacuationPatch(
                "ACESSO AO CORREDOR",
                "A rotina agora decide primeiro se o corredor pode abrir. A lista deve ser pública. Permita a abertura quando não houver risco confirmado ou quando a evacuação for justa.",
                "se nao risco_confirmado() {0} evacuacao_justa():\n" +
                "    abrir_corredor({1})\n" +
                "senao:\n" +
                "    selar_corredor()",
                new[] { new[] { "e", "ou" }, new[] { "lista_reservada()", "lista_publica()" } }, apply),
            _ => EvacuationPatch(
                "REGISTRO DE RISCO",
                "Some uma marca para risco confirmado e outra para evacuação injusta. Sele o corredor apenas com duas marcas. Nos demais casos, libere o acesso pela lista pública.",
                "bloqueio = 0\n" +
                "se risco_confirmado():\n" +
                "    bloqueio = bloqueio + 1\n" +
                "se evacuacao_justa() {0} Falso:\n" +
                "    bloqueio = bloqueio + 1\n" +
                "se bloqueio {1} 2:\n" +
                "    selar_corredor()\n" +
                "senao:\n" +
                "    abrir_corredor(lista_publica())",
                new[] { new[] { "!=", "==" }, new[] { "<", "==", ">" } }, apply)
        };
    }

    private static HermesPatch EvacuationPatch(string title, string brief, string source, string[][] fragments, Action apply)
    {
        return new HermesPatch(title, brief, source, fragments, program =>
        {
            var trace = new StringBuilder();
            bool valid = true;
            foreach (var (risk, fair) in new[] { (true, true), (true, false), (false, false), (false, true) })
            {
                string effect = "corredor inalterado";
                var vm = new HermesVm();
                Read(vm, "risco_confirmado", risk);
                Read(vm, "evacuacao_justa", fair);
                Read(vm, "lista_reservada", "lista reservada");
                Read(vm, "lista_publica", "lista pública");
                Signal(vm, "selar_corredor", () => effect = "corredor selado");
                Register(vm, "abrir_corredor", 1, args =>
                {
                    if (args[0] is not string list)
                        throw new InvalidTypeException("O corredor precisa de uma lista de evacuação.");
                    effect = $"acesso pela {list}";
                    return null;
                });
                vm.Interpret(program);

                bool passed = effect == (risk && !fair ? "corredor selado" : "acesso pela lista pública");
                valid &= passed;
                Append(trace, passed, $"{(risk ? "Risco" : "Sem risco")} · {(fair ? "evacuação justa" : "evacuação injusta")}", effect);
            }
            return new HermesDiagnostic(trace.ToString(), valid ? apply : null);
        });
    }

    public static HermesPatch CalibrateModule(string weapon, int nextLevel, Action apply)
    {
        string module = $"{weapon} · nível {nextLevel}.";
        double required = 24 + (nextLevel - 1) * 6;
        if (nextLevel >= 4)
        {
            return DrawVariant("ModuloAvancado", 3) switch
            {
                0 => CoolingInterlockPatch(module, required, apply),
                1 => ThermalMarginPatch(module, nextLevel, apply),
                _ => VerifyChannelsPatch(module, apply)
            };
        }

        return DrawVariant("ModuloInicial", 3) switch
        {
            0 => EnergyReservePatch(module, required, apply),
            1 => AlignSensorsPatch(module, 11 - nextLevel, apply),
            _ => CombineCellsPatch(module, required, apply)
        };
    }

    private static HermesPatch EnergyReservePatch(string module, double required, Action apply)
    {
        return ModulePatch(
            "RESERVA DE BORDO",
            $"{module} Acople o módulo somente quando a reserva alcançar o consumo de {required:0} u. A reserva no limite já é suficiente.",
            "reserva = ler_reserva()\n" +
            "se reserva {0} consumo_modulo():\n" + InstallModule,
            new[] { new[] { ">", ">=", "<=" } },
            new[] { required - 7, required, required + 10 },
            (vm, reserve) =>
            {
                Read(vm, "ler_reserva", reserve);
                Read(vm, "consumo_modulo", required);
            },
            reserve => reserve >= required, reserve => $"{reserve:0} u disponíveis", apply);
    }

    private static HermesPatch AlignSensorsPatch(string module, double tolerance, Action apply)
    {
        return ModulePatch(
            "ALINHAMENTO DOS SENSORES",
            $"{module} Acople o módulo somente quando o desvio dos sensores estiver dentro da tolerância de {tolerance:0} graus, inclusive no limite.",
            "desvio = ler_desvio()\n" +
            "se desvio {0} tolerancia():\n" + InstallModule,
            new[] { new[] { "<", "<=", ">=" } },
            new[] { tolerance - 3, tolerance, tolerance + 4 },
            (vm, deviation) =>
            {
                Read(vm, "ler_desvio", deviation);
                Read(vm, "tolerancia", tolerance);
            },
            deviation => deviation <= tolerance, deviation => $"Desvio de {deviation:0}°", apply);
    }

    private static HermesPatch CombineCellsPatch(string module, double required, Action apply)
    {
        return ModulePatch(
            "CÉLULAS DE ENERGIA",
            $"{module} A reserva está distribuída em duas células. Some as leituras e acople o módulo quando o total alcançar {required:0} u.",
            "carga = celula_principal() {0} celula_auxiliar()\n" +
            "se carga >= consumo_modulo():\n" + InstallModule,
            new[] { new[] { "-", "+", "*" } },
            new (double Primary, double Auxiliary)[] { (required - 9, 5), (required - 7, 7), (required - 10, 12) },
            (vm, cells) =>
            {
                Read(vm, "celula_principal", cells.Primary);
                Read(vm, "celula_auxiliar", cells.Auxiliary);
                Read(vm, "consumo_modulo", required);
            },
            cells => cells.Primary + cells.Auxiliary >= required,
            cells => $"Principal {cells.Primary:0} u · auxiliar {cells.Auxiliary:0} u", apply);
    }

    private static HermesPatch CoolingInterlockPatch(string module, double required, Action apply)
    {
        return ModulePatch(
            "TRAVA DE REFRIGERAÇÃO",
            $"{module} Acople o módulo somente com pelo menos {required:0} u de reserva e com o circuito refrigerado.",
            "reserva = ler_reserva()\n" +
            "se reserva {0} consumo_modulo() {1} circuito_frio():\n" + InstallModule,
            new[] { new[] { ">", ">=", "<=" }, new[] { "ou", "e" } },
            new (double Reserve, bool Cold)[] { (required - 7, true), (required, true), (required + 10, false), (required + 10, true) },
            (vm, sample) =>
            {
                Read(vm, "ler_reserva", sample.Reserve);
                Read(vm, "consumo_modulo", required);
                Read(vm, "circuito_frio", sample.Cold);
            },
            sample => sample.Reserve >= required && sample.Cold,
            sample => $"{sample.Reserve:0} u · circuito {(sample.Cold ? "frio" : "quente")}", apply);
    }

    private static HermesPatch ThermalMarginPatch(string module, int level, Action apply)
    {
        double limit = 80 + level * 2;
        double margin = 7 + level;
        return ModulePatch(
            "MARGEM TÉRMICA",
            $"{module} Calcule quanto falta até o limite de {limit:0} °C. Acople o módulo somente com folga de {margin:0} °C ou mais e sincronismo confirmado.",
            "margem = limite_termico() {0} temperatura_atual()\n" +
            "se margem >= folga_minima() {1} sincronismo_ok():\n" + InstallModule,
            new[] { new[] { "+", "-", "*" }, new[] { "ou", "e" } },
            new (double Temperature, bool Synced)[]
            {
                (limit - margin + 3, true), (limit - margin, true),
                (limit - margin - 7, false), (limit - margin - 8, true)
            },
            (vm, sample) =>
            {
                Read(vm, "limite_termico", limit);
                Read(vm, "temperatura_atual", sample.Temperature);
                Read(vm, "folga_minima", margin);
                Read(vm, "sincronismo_ok", sample.Synced);
            },
            sample => limit - sample.Temperature >= margin && sample.Synced,
            sample => $"{sample.Temperature:0} °C · sincronismo {(sample.Synced ? "OK" : "ausente")}", apply);
    }

    private static HermesPatch VerifyChannelsPatch(string module, Action apply)
    {
        return ModulePatch(
            "DIAGNÓSTICO DOS CANAIS",
            $"{module} O módulo usa três canais. Conte uma aprovação por canal que passar no teste e acople o módulo somente com três aprovações.",
            "canal = 1\n" +
            "aprovados = 0\n" +
            "enquanto canal <= 3:\n" +
            "    se testar_canal(canal):\n" +
            "        aprovados = aprovados + {0}\n" +
            "    canal = canal + 1\n" +
            "se aprovados {1} 3:\n" + InstallModule,
            new[] { new[] { "2", "1", "0" }, new[] { ">", "==", "<" } },
            new[] { 0b010, 0b011, 0b101, 0b111 },
            (vm, mask) =>
            {
                Register(vm, "testar_canal", 1, args =>
                {
                    int channel = args[0] is double number ? (int)number : 0;
                    return channel >= 1 && channel <= 3 && (mask & (1 << (channel - 1))) != 0;
                });
            },
            mask => mask == 0b111,
            mask => $"Canal 1: {ChannelStatus(mask, 1)} · 2: {ChannelStatus(mask, 2)} · 3: {ChannelStatus(mask, 3)}",
            apply);
    }

    private static string ChannelStatus(int mask, int channel) => (mask & (1 << (channel - 1))) != 0 ? "OK" : "falha";

    private static HermesPatch ModulePatch<T>(string title, string brief, string source, string[][] fragments,
        T[] samples, Action<HermesVm, T> configure, Func<T, bool> shouldInstall, Func<T, string> describe, Action apply)
    {
        return new HermesPatch(title, brief, source, fragments, program =>
        {
            var trace = new StringBuilder();
            bool valid = true;
            foreach (T sample in samples)
            {
                bool installed = false;
                var vm = new HermesVm();
                configure(vm, sample);
                Signal(vm, "acoplar_modulo", () => installed = true);
                Signal(vm, "aguardar_calibracao", () => installed = false);
                vm.Interpret(program);

                bool passed = installed == shouldInstall(sample);
                valid &= passed;
                Append(trace, passed, describe(sample), installed ? "módulo acoplado" : "módulo em espera");
            }
            return new HermesDiagnostic(trace.ToString(), valid ? apply : null);
        });
    }
}
