using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace X12UtilsFRM {
 
    using System.IO;
    using System.Reflection;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System;
    using X12.Specifications;
    using X12.Specifications.Finders;

    namespace x12Test {
        public class specFinder : SpecificationFinder {
            static void Log(String s, [CallerMemberName] string cn = "", [CallerLineNumber] int ln = 0, [CallerFilePath] string fp = "") {

                Trace.WriteLine($"{DateTime.Now.ToString()}-{cn}@{fp.Substring(fp.LastIndexOf('\\') + 1)}:{ln}:{s}");
                Trace.Flush();
            }
            public override TransactionSpecification FindTransactionSpec(string functionalCode, string versionCode, string transactionSetCode) {

                //{PartnerId}-{TransactionId}-{PartnerX12VersionId}Specification.xml
                if (transactionSetCode == "856") {   
                    Assembly a = Assembly.GetExecutingAssembly();
                    string spec = $"{a.GetName().Name}.Resources.{Properties.Settings.Default.PartnerId}-{transactionSetCode}-{Properties.Settings.Default.PartnerX12VersionId}-Specification.xml";
               
                    //       Stream specStream = a.GetManifestResourceStream(spec);
                   
                    Stream specStream = a.GetManifestResourceStream(spec);
                    Log($"SpecStream={specStream}");
                    Log($"SPec={spec}");

                    return TransactionSpecification.Deserialize(new StreamReader(specStream).ReadToEnd());
                }
                else
                    return base.FindTransactionSpec(functionalCode, versionCode, transactionSetCode);
            }
        }
    }

}
