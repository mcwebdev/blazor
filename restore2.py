import json
import os

with open('extracted_overview.txt', 'r', encoding='utf-8') as f:
    lines = f.readlines()

def restore(filename):
    for line in lines:
        if filename in line and '"name":"default_api:write_to_file"' in line:
            try:
                data = json.loads(line)
                if 'tool_calls' in data:
                    for call in data['tool_calls']:
                        if call.get('name') == 'default_api:write_to_file':
                            args = call.get('arguments', {})
                            if args.get('TargetFile', '').endswith(filename):
                                code = args.get('CodeContent', '')
                                code = code.replace('TaskItemStatus.Todo', 'TaskItemStatus.Open')
                                
                                out_path = 'tests/FlowBoard.Tests/' + filename.split('FlowBoard.Tests/')[-1]
                                if 'Tests.cs' in out_path and 'using Xunit;' not in code:
                                    code = 'using Xunit;\n' + code
                                    
                                os.makedirs(os.path.dirname(out_path), exist_ok=True)
                                with open(out_path, 'w', encoding='utf-8') as f_out:
                                    f_out.write(code)
                                print(f'Restored {out_path}')
            except Exception as e:
                pass

restore('NotificationServiceTests.cs')
restore('TestDbFactory.cs')
