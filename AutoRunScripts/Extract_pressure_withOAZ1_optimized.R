rm(list=ls()) #clear R environment

# Run this script with:
# "C:\Program Files\R\R-4.2.0\bin\Rscript.exe" "Y:\temporary_files\JO\keep\AutoRunScripts\Extract_pressure_withOAZ1_optimized.R"

# ============================================================
# OPTIMIZED VERSION of Extract_pressure_withOAZ1.R
# Changes from original:
#   1. Removed redundant library() calls: dplyr and ggplot2 are
#      already loaded as part of tidyverse.
#   2. Simplified date computation using format(Sys.Date(), ...)
#      instead of manual strsplit/unlist on the date string.
#   3. Moved data_summary(), breaks, and log_breaks outside of
#      process_txt_file() so they are created once per session
#      rather than re-evaluated on every file.
#   4. Eliminated repeated scansummary.csv disk reads: results
#      are accumulated in a list and written to disk once at the
#      end instead of one read+write per file.
#   5. Removed setwd() from inside process_txt_file(); explicit
#      path variables are used throughout instead.
#   6. Phase-based parallel msconvert: all conversions are
#      launched asynchronously (wait = FALSE) in Phase 1, then
#      Phase 2 polls for each output txt file and processes it
#      as soon as it appears, so conversion and downstream
#      processing overlap rather than alternating.
# ============================================================

# Optimization 1: load only tidyverse (includes dplyr + ggplot2)
library(tidyverse)
library(cowplot)
library(gridExtra)

required_rscript <- "C:/Program Files/R/R-4.2.0/bin/Rscript.exe"
if (!file.exists(required_rscript)) {
  warning(paste("Expected Rscript not found at", required_rscript, "- continuing with current R session."))
}

# Optimization 2: simplified date computation
yearmonth <- format(Sys.Date(), "%Y_%m")

scansummary_path    <- "Y:/temporary_files/JO/keep/scansummary.csv"
processed_txt_path  <- "Y:/temporary_files/JO/keep/txt_processed"
msdata_root         <- paste0("Y:/msdata/2026/", yearmonth)

if (!dir.exists(processed_txt_path)) {
  dir.create(processed_txt_path, recursive = TRUE)
}

if (!file.exists(scansummary_path)) {
  message("scansummary.csv not found. Waiting briefly in case it is being edited...")
  for (attempt in 1:10) {
    Sys.sleep(3)
    if (file.exists(scansummary_path)) break
  }
}

if (!file.exists(scansummary_path)) {
  message("scansummary.csv is still unavailable, exiting without changes: ", scansummary_path)
  quit(save = "no", status = 0)
}

scansummary <- read.csv(scansummary_path, header = TRUE, check.names = FALSE, stringsAsFactors = FALSE)

if (!dir.exists(msdata_root)) {
  message("Data folder not found, nothing to process: ", msdata_root)
  quit(save = "no", status = 0)
}

mydffull <- list.files(msdata_root, full.names = TRUE, recursive = TRUE, pattern = "\\.raw$")
mydffull <- as.data.frame(mydffull)

if (nrow(mydffull) == 0) {
  message("No raw files found under: ", msdata_root)
  quit(save = "no", status = 0)
}

mydffull$mydffull <- as.character(mydffull$mydffull)
mydffull$filename <- basename(mydffull$mydffull)
mydffull$filename <- gsub("\\.raw$", "", mydffull$filename, ignore.case = TRUE)

if (!"filename" %in% colnames(scansummary)) {
  stop("The scansummary.csv is missing required column: filename")
}

filestoprocess <- mydffull %>% anti_join(scansummary, by = "filename")

# Known bad file skip
filestoprocess <- filestoprocess[filestoprocess$filename != "LUM2_1276868", ]

if (nrow(filestoprocess) == 0) {
  message("No files to process")
  quit(save = "no", status = 0)
}

filestoprocess$filename <- NULL
filestoprocess <- normalizePath(filestoprocess$mydffull, winslash = "/", mustWork = FALSE)

# Define the path to the msconvert executable
msconvert_path <- '"C:/Program Files/ProteoWizard/ProteoWizard 3.0.24164.38d6037/msconvert.exe"'

build_msconvert_cmd <- function(raw_file, out_dir) {
  raw_file_q <- gsub("/", "\\\\", raw_file)
  out_dir_q  <- gsub("/", "\\\\", out_dir)
  paste0(
    msconvert_path,
    ' "', raw_file_q, '" --text --filter "msLevel 42-"',
    ' -o "', out_dir_q, '"'
  )
}

# Optimization 3: constants defined once outside the per-file function
data_summary <- function(x) {
  m <- mean(x)
  return(c(y = m, ymin = m - sd(x), ymax = m + sd(x)))
}

breaks    <- c(1, 10, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 1e10, 1e11, 1e12)
log_breaks <- log(breaks)

# Optimization 4 + 5: no setwd(), no per-file CSV read; returns a row list instead
process_txt_file <- function(txt_file) {
  if (!file.exists(txt_file)) {
    message("TXT not found for processing: ", txt_file)
    return(invisible(NULL))
  }

  txt_dir   <- dirname(txt_file)
  filename1 <- basename(txt_file)
  filename  <- gsub("\\.txt$", "", filename1, ignore.case = TRUE)
  lines     <- readLines(txt_file)

  maxpressure <- NA

  chromatogram_indices <- grep("chromatogram:", lines)
  if (length(chromatogram_indices) < 1) {
    message("Could not parse chromatogram blocks in: ", filename1)
    return(invisible(NULL))
  }

  has_two_chrom <- any(grepl("(2 chromatograms)", lines, fixed = TRUE)) && length(chromatogram_indices) >= 2
  is_oaz1       <- grepl("^OAZ1_", filename)

  parse_pressure_section <- function(section_lines) {
    array_indices <- grep("binary:", section_lines)
    if (length(array_indices) < 2) return(FALSE)

    second1 <- strsplit(section_lines[array_indices[1]], split = " +")
    second2 <- strsplit(section_lines[array_indices[2]], split = " +")

    n    <- length(second1[[1]])
    second <- cbind(
      structure(second1, row.names = c(NA, -n), class = "data.frame"),
      structure(second2, row.names = c(NA, -n), class = "data.frame")
    )
    second <- second[-(1:3), ]
    colnames(second) <- c("time", "pressure")

    second$time     <- as.numeric(paste(unlist(second$time)))
    second$pressure <- as.numeric(paste(unlist(second$pressure)))
    second <- second[!is.na(second$time) & !is.na(second$pressure), ]

    if (nrow(second) == 0) return(FALSE)

    maxpressure  <<- max(second$pressure / 100000, na.rm = TRUE)
    pressureplot <<- ggplot(second, aes(x = time, y = (pressure / 100000))) +
      geom_point(size = .5) +
      labs(title = paste("Pressure Trace - max pressure", maxpressure), y = "Bar") +
      ylim(c(0, max(second$pressure / 100000) + 10))
    TRUE
  }

  # Parse first chromatogram (MS signal)
  if (has_two_chrom) {
    section1 <- lines[chromatogram_indices[1]:(chromatogram_indices[2] - 1)]
  } else {
    section1 <- lines[chromatogram_indices[1]:length(lines)]
  }

  array_indices1 <- grep("binary:", section1)
  if (length(array_indices1) < 3) {
    message("Insufficient MS binary arrays in: ", filename1)
    return(invisible(NULL))
  }

  first1 <- strsplit(section1[array_indices1[1]], split = " +")
  first2 <- strsplit(section1[array_indices1[2]], split = " +")
  first3 <- strsplit(section1[array_indices1[3]], split = " +")

  n <- length(first1[[1]])
  first <- cbind(
    structure(first1, row.names = c(NA, -n), class = "data.frame"),
    structure(first2, row.names = c(NA, -n), class = "data.frame"),
    structure(first3, row.names = c(NA, -n), class = "data.frame")
  )
  first <- first[-(1:3), ]
  colnames(first) <- c("time", "counts", "msorder")

  first$time    <- paste(unlist(first$time))
  first$counts  <- paste(unlist(first$counts))
  first$msorder <- paste(unlist(first$msorder))
  first$counts  <- as.numeric(as.character(first$counts))

  ms1 <- first %>% filter(msorder == 1)
  ms2 <- first %>% filter(msorder == 2)
  ms1$msorder <- NULL
  ms2$msorder <- NULL

  if ("3" %in% first$msorder) {
    ms3 <- first %>% filter(msorder == 3)
    ms3$msorder <- NULL
  }

  filelocationindex <- grep("location:", lines)
  filelocation <- if (length(filelocationindex) > 0) lines[[filelocationindex[1]]] else ""
  filelocation <- gsub(" ", "", filelocation)
  filelocation <- gsub("location:file:///", "", filelocation)

  starttimeindex <- grep("startTimeStamp:", lines)
  starttime <- if (length(starttimeindex) > 0) lines[[starttimeindex[1]]] else ""
  starttime <- gsub(" ", "", starttime)
  starttime <- gsub("startTimeStamp:", "", starttime)

  datetime          <- as.POSIXct(starttime, format = "%Y-%m-%dT%H:%M:%SZ", tz = "UTC")
  formatted_datetime <- format(datetime, "%Y-%m-%d, %H:%M")

  # Pressure chromatogram parsing
  # OAZ1 pump pressure is in section 6 of the standard msconvert text output.
  if (is_oaz1 && length(chromatogram_indices) >= 6) {
    section6     <- lines[chromatogram_indices[6]:length(lines)]
    parsed_oaz1  <- parse_pressure_section(section6)
    # Safety fallback if section 6 is unexpectedly absent/malformed
    if (!parsed_oaz1 && has_two_chrom) {
      section2 <- lines[chromatogram_indices[2]:length(lines)]
      parse_pressure_section(section2)
    }
  } else if (has_two_chrom) {
    section2 <- lines[chromatogram_indices[2]:length(lines)]
    parse_pressure_section(section2)
  }

  # Signal distribution plot
  first_numeric          <- first
  first_numeric$time     <- NULL
  first_numeric$counts   <- as.numeric(first_numeric$counts)
  first_numeric$msorder  <- as.factor(first_numeric$msorder)

  signals <- ggplot(first_numeric, aes(x = msorder, y = log(counts))) +
    geom_violin(trim = FALSE) +
    stat_summary(fun.data = data_summary) +
    labs(title = "Distribution of Signal per MS Level", y = "Intensity") +
    scale_y_continuous(breaks = log_breaks, labels = breaks)

  maxms1     <- signif(as.numeric(max(ms1$counts)), digits = 3)
  maxms1text <- formatC(maxms1, digits = 2, format = "e")
  ms1$time   <- as.numeric(ms1$time)
  ms1$counts <- as.numeric(ms1$counts)
  ms1plot    <- ggplot(ms1, aes(x = time, y = counts)) + geom_line() +
    labs(title = paste(filename, "- MS1 - max signal", maxms1text, ", ", filelocation, ", ", starttime), y = "Intensity")
  numms1 <- as.numeric(length(ms1$counts))

  maxms2     <- signif(as.numeric(max(ms2$counts)), digits = 5)
  maxms2text <- formatC(maxms2, digits = 2, format = "e")
  ms2$time   <- as.numeric(ms2$time)
  ms2$counts <- as.numeric(ms2$counts)
  ms2plot    <- ggplot(ms2, aes(x = time, y = counts)) + geom_line() +
    labs(title = paste("MS2 - max signal", maxms2text), y = "Intensity")
  numms2 <- as.numeric(length(ms2$counts))

  has_pressure_plot <- exists("pressureplot")

  if (exists("ms3")) {
    maxms3     <- signif(as.numeric(max(ms3$counts)), digits = 3)
    maxms3text <- formatC(maxms3, digits = 3, format = "e")
    ms3$time   <- as.numeric(ms3$time)
    ms3$counts <- as.numeric(ms3$counts)
    ms3plot    <- ggplot(ms3, aes(x = time, y = counts)) + geom_line() +
      labs(title = paste("MS3 - max signal", maxms3text), y = "Intensity")

    numms3     <- as.numeric(length(ms3$counts))
    totalscans <- sum(numms1, numms2, numms3)
    pctms1     <- format(numms1 / totalscans * 100, digits = 3)
    pctms2     <- format(numms2 / totalscans * 100, digits = 3)
    pctms3     <- format(numms3 / totalscans * 100, digits = 3)

    scans <- data.frame(
      what  = c("MS1", "MS2", "MS3", "Total"),
      count = c(numms1, numms2, numms3, totalscans),
      pct   = c(pctms1, pctms2, pctms3, "100"),
      stringsAsFactors = FALSE
    )
    scanstable  <- tableGrob(scans)

    top_row    <- if (has_pressure_plot) plot_grid(ms1plot, pressureplot, nrow = 2) else ms1plot
    second_row <- plot_grid(ms2plot, signals, ncol = 2, rel_widths = c(0.75, 0.25))
    third_row  <- plot_grid(ms3plot, scanstable, ncol = 2, rel_widths = c(0.75, 0.25))

    if (has_pressure_plot) {
      combined_plot <- plot_grid(top_row, second_row, third_row, ncol = 1, rel_heights = c(0.6, 0.2, 0.2))
      output_jpg    <- paste0(filename, ".jpg")
    } else {
      combined_plot <- plot_grid(top_row, second_row, third_row, ncol = 1, rel_heights = c(0.33, 0.33, 0.33))
      output_jpg    <- paste0(filename, "_no_pressure.jpg")
    }
  } else {
    totalscans <- sum(numms1, numms2)
    pctms1     <- format(numms1 / totalscans * 100, digits = 3)
    pctms2     <- format(numms2 / totalscans * 100, digits = 3)

    scans <- data.frame(
      what  = c("MS1", "MS2", "Total"),
      count = c(numms1, numms2, totalscans),
      pct   = c(pctms1, pctms2, "100"),
      stringsAsFactors = FALSE
    )
    scanstable  <- tableGrob(scans)

    top_row    <- if (has_pressure_plot) plot_grid(ms1plot, pressureplot, nrow = 2) else ms1plot
    second_row <- plot_grid(ms2plot, signals, scanstable, ncol = 3, rel_widths = c(0.5, 0.3, 0.2))

    if (has_pressure_plot) {
      combined_plot <- plot_grid(top_row, second_row, ncol = 1, rel_heights = c(0.67, 0.33))
      output_jpg    <- paste0(filename, ".jpg")
    } else {
      combined_plot <- plot_grid(top_row, second_row, ncol = 1, rel_heights = c(0.5, 0.5))
      output_jpg    <- paste0(filename, "_no_pressure.jpg")
    }

    pctms3 <- NA
    numms3 <- NA
  }

  jpg_dir <- file.path(txt_dir, "jpg")
  if (!dir.exists(jpg_dir)) {
    dir.create(jpg_dir, recursive = TRUE)
  }

  ggsave(output_jpg, combined_plot, width = 12, height = 8, dpi = 150, path = jpg_dir)

  destination_path <- file.path(processed_txt_path, filename1)
  if (file.exists(destination_path)) {
    file.remove(destination_path)
  }
  file.rename(txt_file, destination_path)

  message("Processed fully: ", filename)

  # Optimization 4: return structured result row instead of writing CSV here
  invisible(list(
    filename           = filename,
    MS1_Scans          = numms1,
    MS2_Scans          = numms2,
    Total_Scans        = totalscans,
    Pct_MS1_of_total   = pctms1,
    Pct_MS2_of_total   = pctms2,
    Max_Pressure       = ifelse(is.na(maxpressure), "NA", as.character(maxpressure)),
    Max_MS1            = maxms1text,
    File_Location      = filelocation,
    Start_Time         = formatted_datetime,
    Pct_MS3_of_total   = ifelse(is.na(pctms3), NA, pctms3),
    MS3_Scans          = ifelse(is.na(numms3), NA, numms3)
  ))
}

# ===========================================================
# Optimization 6: Phase-based parallel processing
#
# Phase 1 — Launch all msconvert conversions simultaneously
#            using wait = FALSE (non-blocking). All conversions
#            run in parallel instead of being serialized.
#
# Phase 2 — Iterate through expected output txt files.
#            For each file, poll until the conversion is done
#            (the txt file appears), then process immediately.
#            Overlaps I/O-bound conversion with CPU-bound R
#            work so later files' conversions finish while
#            earlier ones are being processed.
# ===========================================================

total <- length(filestoprocess)
pb    <- winProgressBar(title = "Per-file Processing", min = 0, max = total, width = 500)

# ---- Phase 1: launch all msconvert processes asynchronously ----
message("Phase 1: Launching all ", total, " msconvert conversions in parallel...")

txt_files_expected <- character(total)

for (i in seq_along(filestoprocess)) {
  raw_file <- filestoprocess[[i]]

  if (!file.exists(raw_file)) {
    message("Raw file missing, skipping launch: ", raw_file)
    next
  }

  raw_dir <- dirname(raw_file)

  jpg_dir <- file.path(raw_dir, "jpg")
  if (!dir.exists(jpg_dir)) {
    dir.create(jpg_dir, recursive = TRUE)
  }

  txt_files_expected[i] <- file.path(
    raw_dir,
    paste0(tools::file_path_sans_ext(basename(raw_file)), ".txt")
  )

  cmd <- build_msconvert_cmd(raw_file, raw_dir)
  # wait = FALSE: launch and return immediately so the next file's
  # conversion can start before this one finishes.
  system(cmd, wait = FALSE)
}

# ---- Phase 2: process txt files as conversions complete ----
message("Phase 2: Processing files as conversions complete...")

max_wait_secs <- 3600  # give each conversion up to 1 hour before giving up
new_rows      <- list()

for (i in seq_along(filestoprocess)) {
  raw_file <- filestoprocess[[i]]
  txt_file <- txt_files_expected[i]

  Sys.sleep(0.05)
  setWinProgressBar(pb, i,
    title = paste0("File Progress ", round(i / total * 100, 0),
                   "%, ", i, " of ", total))
  message("Waiting for conversion ", i, " of ", total, ": ", basename(raw_file))

  if (!file.exists(raw_file) || nchar(txt_file) == 0) {
    next
  }

  # Poll until the expected txt file appears (conversion complete)
  waited <- 0
  while (!file.exists(txt_file) && waited < max_wait_secs) {
    Sys.sleep(5)
    waited <- waited + 5
  }

  if (!file.exists(txt_file)) {
    message("Conversion timed out or failed for: ", raw_file)
    next
  }

  result_row <- tryCatch(
    process_txt_file(txt_file),
    error = function(e) {
      message("Processing error for ", txt_file, ": ", as.character(e))
      NULL
    }
  )

  if (!is.null(result_row)) {
    new_rows <- c(new_rows, list(result_row))
  }
}

# ---- Write scansummary CSV once at the end (Optimization 4) ----
if (length(new_rows) > 0) {
  scansummary_final <- read.csv(
    scansummary_path, header = TRUE, check.names = FALSE, stringsAsFactors = FALSE
  )
  for (row in new_rows) {
    rowpos <- nrow(scansummary_final) + 1
    scansummary_final[rowpos, ] <- NA
    for (col in names(row)) {
      scansummary_final[[col]][rowpos] <- row[[col]]
    }
  }
  try({
    write.csv(scansummary_final, file = scansummary_path, na = "NA", row.names = FALSE)
  })
}

close(pb)
message("Done.")
