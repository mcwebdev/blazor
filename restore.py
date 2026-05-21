import os

with open('extracted_overview.txt', 'r', encoding='utf-8') as f:
    content = f.read()

# We need to split by the tool call name
chunks = content.split('default_api:write_to_file')

def restore(filename):
    for chunk in chunks:
        if filename in chunk:
            # We are looking for CodeContent: <marker> ... <marker>,Description:
            cc_start = chunk.find('CodeContent:')
            desc_start = chunk.find('Description:', cc_start)
            
            if cc_start != -1 and desc_start != -1:
                # The actual code is between CodeContent: <marker> and <marker>,Description:
                # We can just extract everything between them and strip the markers.
                # Let's find the first character after 'CodeContent:' that is not space or punctuation? No, the markers are special.
                # Let's just slice it.
                code = chunk[cc_start + len('CodeContent:'):desc_start]
                # Clean up any trailing/leading markers, commas, spaces
                # A marker looks like \xed\xa0\xbd... let's just strip non-ascii or specific characters if we want, or just strip() and remove the last comma.
                # Since we know it ends with a comma before Description:, let's do:
                code = code.rsplit(',', 1)[0].strip()
                
                # Remove the custom delimiters which are length 1 or so.
                # Actually, if we just slice off the first and last char assuming they are markers:
                if len(code) > 2:
                    code = code[1:-1]
                
                code = code.replace('TaskItemStatus.Todo', 'TaskItemStatus.Open')
                
                out_path = 'tests/FlowBoard.Tests/' + filename.split('FlowBoard.Tests/')[-1]
                if 'Tests.cs' in out_path and 'using Xunit;' not in code:
                    code = 'using Xunit;\n' + code
                    
                os.makedirs(os.path.dirname(out_path), exist_ok=True)
                with open(out_path, 'w', encoding='utf-8') as f_out:
                    f_out.write(code)
                print(f'Restored {out_path}')

restore('NotificationServiceTests.cs')
restore('TestDbFactory.cs')
