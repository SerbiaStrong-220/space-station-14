forensic-scanner-interface-storage-micro-fibers = Microfibers
forensic-pad-verb-text = Get samples
forensic-pad-verb-message = Perform a samples extraction
forensic-pad-verb-no-sapmles = Object don't have any samples to take
forensic-scanner-match-storage-micro-fiber = Match in microfiber found!
glove-micro-fiber-scanner-verb-text = Collect a scanner sample
glove-micro-fiber-scanner-verb-message = Scan the object in the selected mode and dispense a sample swab.
glove-micro-fiber-scanner-mode-surface = microfiber collection
glove-micro-fiber-scanner-mode-own = item sample
glove-micro-fiber-scanner-mode-label = Mode
glove-micro-fiber-scanner-mode-verb = Select mode
glove-micro-fiber-scanner-mode-tooltip = Microfibers collects samples of gloves, clothing and equipment from the object's surface. Sample reads the selected pair of gloves or item's own individual code; an object without a sample will be refused.
glove-micro-fiber-scanner-mode-surface-short = Microfibers
glove-micro-fiber-scanner-mode-own-short = Sample
glove-micro-fiber-scanner-mode-selected = Scanner mode: { $mode }.
glove-micro-fiber-scanner-switch-hint = Use the scanner in hand to open its radial mode menu.
glove-micro-fiber-scanner-no-own-sample = Scan refused: the object has no individual sample of its own.
glove-micro-fiber-scanner-mode-changed = Scan interrupted: the scanner mode changed.
glove-micro-fiber-swab-unused = This swab contains no sample. Collected samples are dispensed by the microfiber scanner.
glove-micro-fiber-swab-used = This swab contains a collected sample. Insert it into a microfiber analyzer to examine the sample.
glove-micro-fiber-swab-label = microfiber sample: { $source }
glove-micro-fiber-swab-source = Sample source: { $source }
micro-fiber-analyzer-slot-name = Microfiber sample
micro-fiber-analyzer-interface-title = Microfiber Analyzer
micro-fiber-analyzer-interface-number = № { $number }
micro-fiber-analyzer-interface-number-caption = SERIAL
micro-fiber-analyzer-interface-number-caption-second-line = NUMBER
micro-fiber-analyzer-interface-number-tooltip = Analyzer number: { $number }
micro-fiber-analyzer-interface-brand = NT
micro-fiber-analyzer-interface-eject = Eject sample
micro-fiber-analyzer-interface-print = Print
micro-fiber-analyzer-interface-analyze = Analyze sample
micro-fiber-analyzer-interface-analyzing = Analyzing the sample…
micro-fiber-analyzer-interface-ready = The sample is ready for analysis.
micro-fiber-analyzer-interface-analysis-interrupted = Analysis interrupted: the sample was removed.
micro-fiber-analyzer-interface-description = Description
micro-fiber-analyzer-interface-description-placeholder = Enter a sample description…
micro-fiber-analyzer-interface-save-description = Save
micro-fiber-analyzer-interface-description-value = Description: { $description }
micro-fiber-analyzer-interface-history = History
micro-fiber-analyzer-interface-compare-history = Compare samples
micro-fiber-analyzer-interface-compare-first = First sample
micro-fiber-analyzer-interface-compare-second = Second sample
micro-fiber-analyzer-interface-compare-run = Compare
micro-fiber-analyzer-interface-compare-instructions = Select two samples from the history and press Compare.
micro-fiber-analyzer-interface-compare-same-sample = Select two different samples.
micro-fiber-analyzer-interface-compare-profile-count = Matching glove sample codes: { $count }
micro-fiber-analyzer-interface-compare-storage-micro-fiber-count = Matching storage sample codes: { $count }
micro-fiber-analyzer-interface-compare-no-profiles = No matching glove sample codes.
micro-fiber-analyzer-interface-compare-no-storage-micro-fibers = No matching storage sample codes.
micro-fiber-analyzer-interface-compare-note = A matching sample code identifies a specific pair of gloves or individual storage item, but does not establish its owner.
micro-fiber-analyzer-interface-compare-not-enough = Comparison requires two analyzed samples in the history.
micro-fiber-analyzer-interface-current-sample = Unexamined sample
micro-fiber-analyzer-interface-history-empty = History is empty.
micro-fiber-analyzer-interface-history-sample = Sample { $identifier }
micro-fiber-analyzer-interface-source = Examined object: { $source }
micro-fiber-analyzer-interface-source-heading = { $source }
micro-fiber-analyzer-interface-analyzed-at = Analysis time: { $time }
micro-fiber-analyzer-interface-printer-ready = The linked photocopier is ready to print.
micro-fiber-analyzer-interface-printer-unavailable = The photocopier is linked but unavailable. Check power, toner and its print queue.
micro-fiber-analyzer-interface-printer-disconnected = No photocopier is linked. Establish a link with a multitool.
micro-fiber-analyzer-port-source-name = Microfiber analysis report
micro-fiber-analyzer-port-source-description = Sends the report to the linked photocopier.
micro-fiber-analyzer-port-sink-name = Print documents
micro-fiber-analyzer-port-sink-description = Receives reports from a linked analyzer for printing.
micro-fiber-analyzer-report-title = Microfiber analysis report
micro-fiber-analyzer-comparison-report-title = Microfiber comparison report
micro-fiber-analyzer-comparison-report-header = [bold]Form NT-SEC-MFC[/bold]
    { "[head=2]Microfiber Comparison Report[/head]" }
micro-fiber-analyzer-comparison-report-sample = [head=3]Examined Sample No. { $number }[/head]
    { "[bold]Registration number:[/bold]" } { $identifier }
micro-fiber-analyzer-comparison-report-matches = [head=2]Matching Sample Codes[/head]
micro-fiber-analyzer-report-header = [bold]Form NT-SEC-GTE[/bold]
    { "[head=2]Microfiber Analysis Report[/head]" }

    { "[bold]Registration number:[/bold]" } { $identifier }
micro-fiber-analyzer-report-source = [bold]Examined object:[/bold] { $source }
micro-fiber-analyzer-report-analyzed-at = [bold]Analysis completed (elapsed round time):[/bold] { $time }
micro-fiber-analyzer-report-results = [head=3]Identified Glove Samples[/head]
    { "[bold]Number of glove samples:[/bold]" } { $count }
micro-fiber-analyzer-report-trace = [bold]Sample No. { $number }[/bold]
    { "[bold]Glove type:[/bold]" } { $type }
    { "[bold]Sample code:[/bold]" } { $code }
micro-fiber-analyzer-report-storage-micro-fibers = [head=3]Identified Storage Samples[/head]
    { "[bold]Number of storage samples:[/bold]" } { $count }
micro-fiber-analyzer-report-storage-micro-fiber = [bold]Storage Sample No. { $number }[/bold]
    { "[bold]Storage item:[/bold]" } { $type }
    { "[bold]Sample code:[/bold]" } { $code }
micro-fiber-analyzer-report-description = [head=3]Sample Description[/head]
    { $description }
micro-fiber-analyzer-report-footer = [italic]This document was generated automatically by a microfiber analyzer.[/italic]

    { "[italic]Space for the detective's signature and stamp[/italic]" }
micro-fiber-analyzer-interface-no-swab = Insert a swab containing a sample.
micro-fiber-analyzer-interface-unused-swab = The inserted swab has not been used yet.
micro-fiber-analyzer-interface-traces = Glove samples
micro-fiber-analyzer-interface-no-traces = No glove samples found.
micro-fiber-analyzer-interface-storage-micro-fibers = Storage samples
micro-fiber-analyzer-interface-no-storage-micro-fibers = No storage samples were found.
micro-fiber-analyzer-interface-type = Glove type: { $type }
micro-fiber-analyzer-interface-item-type = Storage item: { $type }
micro-fiber-analyzer-interface-code = Sample code: { $code }
micro-fiber-analyzer-interface-glove-sample-number = Glove sample No. { $number }
micro-fiber-analyzer-interface-storage-sample-number = Storage sample No. { $number }
micro-fiber-analyzer-interface-empty-category = None found
micro-fiber-analyzer-interface-unknown-type = Unknown glove type
micro-fiber-analyzer-interface-unknown-item = Unknown storage item
