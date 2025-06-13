#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
代码行数统计工具
支持多种编程语言的代码行数统计，包括总行数、空行数、注释行数和有效代码行数
"""

import os
import re
import argparse
from collections import defaultdict
from pathlib import Path

class LineCounter:
    def __init__(self):
        # 支持的文件扩展名和对应的语言
        self.language_extensions = {
            '.py': 'Python',
            '.js': 'JavaScript',
            '.ts': 'TypeScript',
            '.java': 'Java',
            '.c': 'C',
            '.cpp': 'C++',
            '.cc': 'C++',
            '.cxx': 'C++',
            '.h': 'C/C++ Header',
            '.hpp': 'C++ Header',
            '.cs': 'C#',
            '.php': 'PHP',
            '.rb': 'Ruby',
            '.go': 'Go',
            '.rs': 'Rust',
            '.swift': 'Swift',
            '.kt': 'Kotlin',
            '.scala': 'Scala',
            '.sh': 'Shell',
            '.bash': 'Bash',
            '.zsh': 'Zsh',
            '.fish': 'Fish',
            '.ps1': 'PowerShell',
            '.r': 'R',
            '.R': 'R',
            '.m': 'Objective-C/MATLAB',
            '.mm': 'Objective-C++',
            '.pl': 'Perl',
            '.lua': 'Lua',
            '.dart': 'Dart',
            '.elm': 'Elm',
            '.ex': 'Elixir',
            '.exs': 'Elixir',
            '.clj': 'Clojure',
            '.hs': 'Haskell',
            '.ml': 'OCaml',
            '.fs': 'F#',
            '.vb': 'Visual Basic',
            '.pas': 'Pascal',
            '.d': 'D',
            '.nim': 'Nim',
            '.cr': 'Crystal',
            '.jl': 'Julia',
            '.zig': 'Zig'
        }
        
        # 不同语言的注释模式
        self.comment_patterns = {
            'single_line': {
                '#': ['.py', '.sh', '.bash', '.zsh', '.fish', '.r', '.R', '.pl', '.nim', '.jl'],
                '//': ['.js', '.ts', '.java', '.c', '.cpp', '.cc', '.cxx', '.h', '.hpp', '.cs', '.php', '.go', '.rs', '.swift', '.kt', '.scala', '.dart', '.d', '.zig'],
                ';': ['.clj'],
                '--': ['.hs', '.elm', '.lua'],
                '%': ['.m'],  # MATLAB
                "'": ['.vb'],
                '(*': ['.ml', '.fs', '.pas']
            },
            'multi_line': {
                ('/*', '*/'): ['.js', '.ts', '.java', '.c', '.cpp', '.cc', '.cxx', '.h', '.hpp', '.cs', '.php', '.go', '.rs', '.swift', '.kt', '.scala', '.dart', '.d'],
                ('"""', '"""'): ['.py'],
                ("'''", "'''"): ['.py'],
                ('=begin', '=end'): ['.rb'],
                ('--[[', '--]]'): ['.lua'],
                ('{-', '-}'): ['.hs', '.elm'],
                ('(*', '*)'): ['.ml', '.fs', '.pas']
            }
        }
    
    def is_comment_line(self, line, file_ext):
        """判断是否为注释行"""
        line = line.strip()
        if not line:
            return False
        
        # 检查单行注释
        for comment_char, extensions in self.comment_patterns['single_line'].items():
            if file_ext in extensions and line.startswith(comment_char):
                return True
        
        return False
    
    def count_lines_in_file(self, file_path):
        """统计单个文件的行数"""
        try:
            with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                lines = f.readlines()
        except Exception as e:
            print(f"警告: 无法读取文件 {file_path}: {e}")
            return None
        
        total_lines = len(lines)
        empty_lines = 0
        comment_lines = 0
        code_lines = 0
        
        file_ext = Path(file_path).suffix.lower()
        in_multiline_comment = False
        multiline_start = None
        multiline_end = None
        
        # 检查是否有多行注释模式
        for (start, end), extensions in self.comment_patterns['multi_line'].items():
            if file_ext in extensions:
                multiline_start = start
                multiline_end = end
                break
        
        for line in lines:
            stripped_line = line.strip()
            
            # 空行
            if not stripped_line:
                empty_lines += 1
                continue
            
            # 检查多行注释
            if multiline_start and multiline_end:
                if not in_multiline_comment and multiline_start in stripped_line:
                    in_multiline_comment = True
                    if multiline_end in stripped_line and stripped_line.index(multiline_end) > stripped_line.index(multiline_start):
                        in_multiline_comment = False
                    comment_lines += 1
                    continue
                elif in_multiline_comment:
                    if multiline_end in stripped_line:
                        in_multiline_comment = False
                    comment_lines += 1
                    continue
            
            # 检查单行注释
            if self.is_comment_line(stripped_line, file_ext):
                comment_lines += 1
            else:
                code_lines += 1
        
        return {
            'total': total_lines,
            'empty': empty_lines,
            'comment': comment_lines,
            'code': code_lines
        }
    
    def scan_directory(self, directory, exclude_dirs=None, include_hidden=False):
        """扫描目录下的所有代码文件"""
        if exclude_dirs is None:
            exclude_dirs = {'.git', '.svn', '.hg', '__pycache__', 'node_modules', '.vscode', '.idea', 'build', 'dist', 'target'}
        
        results = defaultdict(lambda: {
            'files': 0,
            'total': 0,
            'empty': 0,
            'comment': 0,
            'code': 0
        })
        
        file_details = []
        
        for root, dirs, files in os.walk(directory):
            # 过滤排除的目录
            dirs[:] = [d for d in dirs if d not in exclude_dirs]
            
            # 如果不包含隐藏文件，过滤隐藏目录
            if not include_hidden:
                dirs[:] = [d for d in dirs if not d.startswith('.')]
            
            for file in files:
                # 如果不包含隐藏文件，跳过隐藏文件
                if not include_hidden and file.startswith('.'):
                    continue
                
                file_path = os.path.join(root, file)
                file_ext = Path(file).suffix.lower()
                
                if file_ext in self.language_extensions:
                    language = self.language_extensions[file_ext]
                    line_count = self.count_lines_in_file(file_path)
                    
                    if line_count:
                        results[language]['files'] += 1
                        results[language]['total'] += line_count['total']
                        results[language]['empty'] += line_count['empty']
                        results[language]['comment'] += line_count['comment']
                        results[language]['code'] += line_count['code']
                        
                        file_details.append({
                            'path': file_path,
                            'language': language,
                            'lines': line_count
                        })
        
        return dict(results), file_details
    
    def print_summary(self, results, file_details, show_files=False):
        """打印统计结果"""
        if not results:
            print("未找到任何代码文件")
            return
        
        print("\n" + "="*80)
        print("代码行数统计结果")
        print("="*80)
        
        # 按语言统计
        print(f"\n{'语言':<15} {'文件数':<8} {'总行数':<10} {'空行数':<10} {'注释行数':<12} {'代码行数':<10}")
        print("-" * 80)
        
        total_files = 0
        total_lines = 0
        total_empty = 0
        total_comments = 0
        total_code = 0
        
        # 按代码行数排序
        sorted_results = sorted(results.items(), key=lambda x: x[1]['code'], reverse=True)
        
        for language, stats in sorted_results:
            print(f"{language:<15} {stats['files']:<8} {stats['total']:<10} {stats['empty']:<10} {stats['comment']:<12} {stats['code']:<10}")
            total_files += stats['files']
            total_lines += stats['total']
            total_empty += stats['empty']
            total_comments += stats['comment']
            total_code += stats['code']
        
        print("-" * 80)
        print(f"{'总计':<15} {total_files:<8} {total_lines:<10} {total_empty:<10} {total_comments:<12} {total_code:<10}")
        
        # 显示详细文件信息
        if show_files and file_details:
            print("\n" + "="*80)
            print("文件详细信息")
            print("="*80)
            
            # 按代码行数排序
            sorted_files = sorted(file_details, key=lambda x: x['lines']['code'], reverse=True)
            
            for file_info in sorted_files:
                rel_path = os.path.relpath(file_info['path'])
                lines = file_info['lines']
                print(f"\n文件: {rel_path}")
                print(f"语言: {file_info['language']}")
                print(f"总行数: {lines['total']}, 空行: {lines['empty']}, 注释: {lines['comment']}, 代码: {lines['code']}")

def main():
    parser = argparse.ArgumentParser(description='代码行数统计工具')
    parser.add_argument('directory', nargs='?', default='.', help='要统计的目录路径 (默认: 当前目录)')
    parser.add_argument('-f', '--files', action='store_true', help='显示每个文件的详细信息')
    parser.add_argument('-a', '--all', action='store_true', help='包含隐藏文件和目录')
    parser.add_argument('-e', '--exclude', nargs='*', default=[], help='要排除的目录名')
    
    args = parser.parse_args()
    
    if not os.path.exists(args.directory):
        print(f"错误: 目录 '{args.directory}' 不存在")
        return
    
    counter = LineCounter()
    
    # 默认排除目录加上用户指定的排除目录
    exclude_dirs = {'.git', '.svn', '.hg', '__pycache__', 'node_modules', '.vscode', '.idea', 'build', 'dist', 'target'}
    exclude_dirs.update(args.exclude)
    
    print(f"正在扫描目录: {os.path.abspath(args.directory)}")
    if exclude_dirs:
        print(f"排除目录: {', '.join(sorted(exclude_dirs))}")
    
    results, file_details = counter.scan_directory(
        args.directory, 
        exclude_dirs=exclude_dirs,
        include_hidden=args.all
    )
    
    counter.print_summary(results, file_details, show_files=args.files)

if __name__ == '__main__':
    main()